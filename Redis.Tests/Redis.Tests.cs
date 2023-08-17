using System.Runtime.CompilerServices;
using FluentAssertions;
using NUnit.Framework;
using StackExchange.Redis;

namespace Redis.Tests
{
    public class RedisTests : TestBase
    {
        [Test]
        public async Task StreamReadAsync_WhenThreeValuesAddedToStream_ExpectThreeValuesReturned()
        {
            //arrange
            var options = ConfigurationOptions.Parse(RedisTestHost);
            await using var conn = await ConnectionMultiplexer.ConnectAsync(options);
            var db = conn.GetDatabase();
            var key = GetUniqueKey();

            var id1 = await db.StreamAddAsync(key, "field1", "value1");
            var id2 = await db.StreamAddAsync(key, "field2", "value2");
            var id3 = await db.StreamAddAsync(key, "field3", "value3");

            //act
            var entries = await db.StreamReadAsync(key, StreamPosition.Beginning);

            //assert
            entries.Length.Should().Be(3);
            entries[0].Id.Should().Be(id1);
            entries[1].Id.Should().Be(id2);
            entries[2].Id.Should().Be(id3);
        }

        [Test]
        public async Task StreamReadGroupAsync_WhenThreeValuesAddedToStream_ExpectThreeValuesReturned()
        {
            //arrange
            var options = ConfigurationOptions.Parse(RedisTestHost);
            await using var conn1 = await ConnectionMultiplexer.ConnectAsync(options);
            var db1 = conn1.GetDatabase();
            var key = GetUniqueKey();
            const string groupName = "test_group";

            var id1 = await db1.StreamAddAsync(key, "field1", "value1");
            var id2 = await db1.StreamAddAsync(key, "field2", "value2");
            var id3 = await db1.StreamAddAsync(key, "field3", "value3");

            await db1.StreamCreateConsumerGroupAsync(key, groupName, StreamPosition.Beginning);

            //act
            var entries = await db1.StreamReadGroupAsync(key, groupName, Guid.NewGuid().ToString(), StreamPosition.NewMessages);

            //assert
            entries.Length.Should().Be(3);
            entries[0].Id.Should().Be(id1);
            entries[1].Id.Should().Be(id2);
            entries[2].Id.Should().Be(id3);
        }

        [Test]
        public async Task StreamReadGroupAsync_WhenThreeValuesAddedToStreamWithOneToReturnThenTwoToReturn_ExpectOneThenTwo()
        {
            //arrange
            var options = ConfigurationOptions.Parse(RedisTestHost);
            await using var conn1 = await ConnectionMultiplexer.ConnectAsync(options);
            var db1 = conn1.GetDatabase();
            await using var conn2 = await ConnectionMultiplexer.ConnectAsync(options);
            var db2 = conn2.GetDatabase();
            var key = GetUniqueKey();
            const string groupName = "test_group";

            var id1 = await db1.StreamAddAsync(key, "field1", "value1");
            var id2 = await db1.StreamAddAsync(key, "field2", "value2");
            var id3 = await db1.StreamAddAsync(key, "field3", "value3");

            await db1.StreamCreateConsumerGroupAsync(key, groupName, StreamPosition.Beginning);

            //act
            var entries1 = await db1.StreamReadGroupAsync(key, groupName, Guid.NewGuid().ToString(), StreamPosition.NewMessages, 1);
            var entries2 = await db2.StreamReadGroupAsync(key, groupName, Guid.NewGuid().ToString(), StreamPosition.NewMessages);

            //assert
            entries1.Length.Should().Be(1);
            entries1[0].Id.Should().Be(id1);
            entries2[0].Id.Should().Be(id2);
            entries2[1].Id.Should().Be(id3);
        }

        [Test]
        public async Task PubSub_WhenTestMultipleSubscribersAndGetMessage_AssertReceivedMessageCounts()
        {
            //arrange
            var options = ConfigurationOptions.Parse(RedisTestHost);
            await using var subscriberConn1 = await ConnectionMultiplexer.ConnectAsync(options);
            await using var subscriberConn2 = await ConnectionMultiplexer.ConnectAsync(options);
            await using var publisherConn = await ConnectionMultiplexer.ConnectAsync(options);

            var channelId = RedisChannel.Literal(Me());
            var subscriber1 = subscriberConn1.GetSubscriber();
            var subscriber2 = subscriberConn2.GetSubscriber();
            publisherConn.GetDatabase().Ping();
            var pub = publisherConn.GetSubscriber();
            
            int subscriber1Count = 0, subscriber2Count = 0;

            var t1 = subscriber1.SubscribeAsync(channelId, (_, msg) => { if (msg == "message") Interlocked.Increment(ref subscriber1Count); });
            var t2 = subscriber2.SubscribeAsync(channelId, (_, msg) => { if (msg == "message") Interlocked.Increment(ref subscriber2Count); });
            await Task.WhenAll(t1, t2).ForAwait();

            //act
            var publishCount = await pub.PublishAsync(channelId, "message");
            
            //assert
            publishCount.Should().Be(2);
            await AllowReasonableTimeToPublishAndProcess().ForAwait();
            Interlocked.CompareExchange(ref subscriber1Count, 0, 0).Should().Be(1);
            Interlocked.CompareExchange(ref subscriber2Count, 0, 0).Should().Be(1);
            
            //act
            // unsubscribe the first subscriber
            await subscriber1.UnsubscribeAsync(channelId);
            publishCount = await pub.PublishAsync(channelId, "message");
            
            //assert
            publishCount.Should().Be(1);
            await AllowReasonableTimeToPublishAndProcess().ForAwait();
            Interlocked.CompareExchange(ref subscriber1Count, 0, 0).Should().Be(1);
            Interlocked.CompareExchange(ref subscriber2Count, 0, 0).Should().Be(2);
        }
        
        [Test]
        public async Task StreamReadAsync_WhenTwentyValuesAddedToStreamWithALimitOfTen_ExpectLastTenValuesReturned()
        {
            //arrange
            var options = ConfigurationOptions.Parse(RedisTestHost);
            await using var conn = await ConnectionMultiplexer.ConnectAsync(options);
            var db = conn.GetDatabase();
            var key = GetUniqueKey();

            var redisValues = new List<RedisValue>();
            for (var i = 0; i < 20; i++)
            {
                redisValues.Add(await db.StreamAddAsync(key, $"field{i}", $"value{i}", maxLength: 10));
            }
            
            //act
            var entries = await db.StreamReadAsync(key, StreamPosition.Beginning);

            //assert
            entries.Length.Should().Be(10);
            for (var i = 0; i < 10; i++)
            {
                entries[i].Id.Should().Be(redisValues[i + 10]);
            }
        }
        
        [Test]
        public async Task StreamReadAsync_WhenThreeValuesAddedToStreamAndThenOneReadThenTwo_ExpectThreeValuesReturnedWithContinuance()
        {
            //arrange
            var options = ConfigurationOptions.Parse(RedisTestHost);
            await using var conn = await ConnectionMultiplexer.ConnectAsync(options);
            var db = conn.GetDatabase();
            var key = GetUniqueKey();

            var id1 = await db.StreamAddAsync(key, "field1", "value1");
            var id2 = await db.StreamAddAsync(key, "field2", "value2");
            var id3 = await db.StreamAddAsync(key, "field3", "value3");

            //act
            var firstReadEntries = await db.StreamReadAsync(key, StreamPosition.Beginning, 1);
            var secondReadEntries = await db.StreamReadAsync(key, firstReadEntries[0].Id);

            //assert
            firstReadEntries.Length.Should().Be(1);
            secondReadEntries.Length.Should().Be(2);
            firstReadEntries[0].Id.Should().Be(id1);
            secondReadEntries[0].Id.Should().Be(id2);
            secondReadEntries[1].Id.Should().Be(id3);
        }

        private static RedisKey GetUniqueKey([CallerMemberName] string type = null) => $"{type}_StreamTest_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        private static string Me([CallerFilePath] string filePath = null, [CallerMemberName] string caller = null) =>
            Environment.Version + "." + Path.GetFileNameWithoutExtension(filePath) + "-" + caller;
        
        private static Task AllowReasonableTimeToPublishAndProcess() => Task.Delay(500);
    }
}