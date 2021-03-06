﻿// Copyright 2011-2020 Trent Tobler.All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;

namespace TrentTobler.Collections.Sorted.Tests
{
    [TestFixture]
    public class BTreeDictionaryTests
    {
        static Random rand = new Random( 101 );
        
        static ReadOnlyCollection<KeyValuePair<int, int>> sampleList = new ReadOnlyCollection<KeyValuePair<int, int>>(
            (
                from i in Enumerable.Range( 0, 1000 )
                select new KeyValuePair<int,int>( i * 10, rand.Next( 1000 ) )
            ).Shuffle()
            .ToArray() );

        static ReadOnlyCollection<KeyValuePair<int, int>> sortedList = new ReadOnlyCollection<KeyValuePair<int, int>>( 
            sampleList
            .OrderBy( i => i.Key )
            .ToArray() );

        static KeyValuePair<int,int>[] CreateRandomHalf()
        {
            var half = sampleList.Choose( sampleList.Count / 2, sampleList.Count ).ToArray();
            Assert.IsNotEmpty( half );
            return half;
        }

        static BTreeDictionary<int,int> CreateSampleTree()
        {
            var b = new BTreeDictionary<int,int>( 10 );
            b.AddRange( sampleList );
            return b;
        }

		#region Tests

		[Test]
		public void TestRemoveFirst()
		{
			var instance = new BTreeDictionary<int, int>( 3 );
			instance.Add( 1, 1 );
			instance.Add( 2, 2 );
			instance.Add( 3, 3 );
			instance.Add( 4, 4 );
			instance.Add( 5, 5 );
			instance.Add( 6, 6 );
			instance.Add( 7, 7 );
			instance.Add( 8, 8 );
			instance.Add( 9, 9 );

			instance.Remove( 3 );
			instance.RemoveAt( 0 );

			Assert.AreEqual( "2,4,5,6,7,8,9", string.Join( ",", instance.Keys ) );
		}

        [Test]
        public void AllowDuplicates()
        {
            var duplicates = new List<KeyValuePair<int,int>>();
            for( int i = 0; i < 1000; ++i )
                duplicates.Add( new KeyValuePair<int,int>( rand.Next( 100 ), i ) );

            var orderedDuplicates = new List<KeyValuePair<int,int>>(
                from item in duplicates
                orderby item.Key, item.Value
                select item );

            var b = new BTreeDictionary<int,int>( 10 )
            {
                AllowDuplicates = true,
                InsertBias = 1,
                LookupBias = -1,
                RemoveBias = -1,
            };

            // Test insertion with forward bias.
            b.InsertBias = 1;
            b.AddRange( duplicates );

            b.AssertEqual( orderedDuplicates );
            Assert.IsFalse( b.WhereLessOrEqualBackwards( -1 ).Any() );
            Assert.IsFalse( b.WhereGreaterOrEqual( 1001 ).Any() );
            b.WhereGreaterOrEqual( -1 ).AssertEqual( orderedDuplicates );
            b.WhereLessOrEqualBackwards( 1001 ).Reverse().AssertEqual( orderedDuplicates );

            for( int i = 1; i < orderedDuplicates.Count; ++i )
            {
                var prev = orderedDuplicates[i - 1];
                var here = orderedDuplicates[i];
                if( prev.Key != here.Key )
                {
                    Assert.IsTrue( b.ContainsKey( prev.Key ) );
                    Assert.IsTrue( b.ContainsKey( here.Key ) );

                    int orderedLast = i + orderedDuplicates.Skip( i ).TakeWhile( item => item.Key == here.Key ).Count() - 1;
                    Assert.AreEqual( here, b.WhereGreaterOrEqual( here.Key ).First() );
                    Assert.AreEqual( orderedDuplicates[orderedLast], b.WhereLessOrEqualBackwards( here.Key ).First() );
                    b.WhereGreaterOrEqual( here.Key ).AssertEqual( orderedDuplicates.Skip( i ) );
                    b.WhereLessOrEqualBackwards( prev.Key ).AssertEqual( orderedDuplicates.Take( i ).Reverse() );
                }
            }

            // Test insertion with reverse insert bias.
            b.Clear();
            b.InsertBias = -1;
            b.AddRange( duplicates );
            orderedDuplicates = new List<KeyValuePair<int, int>>(
                from item in duplicates
                orderby item.Key, item.Value descending
                select item );

            b.AssertEqual( orderedDuplicates );
            Assert.IsFalse( b.WhereLessOrEqualBackwards( -1 ).Any() );
            Assert.IsFalse( b.WhereGreaterOrEqual( 1001 ).Any() );
            b.WhereGreaterOrEqual( -1 ).AssertEqual( orderedDuplicates );
            b.WhereLessOrEqualBackwards( 1001 ).Reverse().AssertEqual( orderedDuplicates );

            for( int i = 1; i < orderedDuplicates.Count; ++i )
            {
                var prev = orderedDuplicates[i - 1];
                var here = orderedDuplicates[i];
                if( prev.Key != here.Key )
                {
                    Assert.IsTrue( b.ContainsKey( prev.Key ) );
                    Assert.IsTrue( b.ContainsKey( here.Key ) );

                    int orderedLast = i + orderedDuplicates.Skip( i ).TakeWhile( item => item.Key == here.Key ).Count() - 1;
                    Assert.AreEqual( here, b.WhereGreaterOrEqual( here.Key ).First() );
                    Assert.AreEqual( orderedDuplicates[orderedLast], b.WhereLessOrEqualBackwards( here.Key ).First() );
                    b.WhereGreaterOrEqual( here.Key ).AssertEqual( orderedDuplicates.Skip( i ) );
                    b.WhereLessOrEqualBackwards( prev.Key ).AssertEqual( orderedDuplicates.Take( i ).Reverse() );
                }
            }
        }

        [Test]
        public void Add()
        {
            CreateSampleTree().AssertEqual( sortedList );
        }

        [Test]
        public void At()
        {
            var b = CreateSampleTree();
            Enumerable.Range( 0, b.Count ).Select( i => b.At( i ) ).AssertEqual( sortedList );
        }

        [Test]
        public void KeyIndex()
        {
            var b = CreateSampleTree();
            sortedList.Select( i => i.Value ).AssertEqual( sortedList.Select( i => b[i.Key] ) );

            var n = sortedList.Select( i => new KeyValuePair<int, int>( i.Key, i.Value + 1 ) ).ToArray();
            n.ForEach( i => b[i.Key] = i.Value );
            n.Select( i => i.Value ).AssertEqual( n.Select( i => b[i.Key] ) );
        }

        [Test]
        public void TryGetValue()
        {
            var b = CreateSampleTree();
            var min = b.Keys.Min();
            var max = b.Keys.Max();
            var t = new Dictionary<int, int>( b );
            var n = Enumerable.Range( min, max - min );
            n.Select(
                i =>
                {
                    int v;
                    var q = b.TryGetValue( i, out v );
                    return Tuple.Create( q, v, i );
                } )
                .AssertEqual(
                n.Select(
                    i =>
                    {
                        int v;
                        var q = t.TryGetValue( i, out v );
                        return Tuple.Create( q, v, i );
                    } ) );
        }

        [Test]
        public void SetValueAt()
        {
            var b = CreateSampleTree();
            var n = Enumerable.Range( 0, b.Count ).Shuffle();
            var cg = sortedList.ToArray();
            foreach( var i in n )
            {
                var r = rand.Next( 1000 );
                cg[i] = new KeyValuePair<int, int>( cg[i].Key, r );
                b.SetValueAt( i, r );
                b.AssertEqual( cg );
            }
        }

        [Test]
        public void ContainsKey()
        {
            var b = CreateSampleTree();
            sampleList.Where( i => !b.ContainsKey( i.Key ) ).AssertEqual( Enumerable.Empty<KeyValuePair<int,int>>() );
            sampleList.Where( i => b.ContainsKey( i.Key + 1 ) ).AssertEqual( Enumerable.Empty<KeyValuePair<int, int>>() );
        }

        [Test]
        public void Remove()
        {
            var b = CreateSampleTree();
            var half = CreateRandomHalf();
            half.ForEach( i => b.Remove( i.Key ) );
            sortedList.Where( i => !half.Contains( i ) ).AssertEqual( b );
        }

        [Test]
        public void Clear()
        {
            var b = CreateSampleTree();

            for( int i = 0; i < 10; ++i )
            {
                b.Clear();
                b.AssertEqual( Enumerable.Empty<KeyValuePair<int, int>>() );

                var s = CreateRandomHalf();
                b.AddRange( s );
                b.AssertEqual( s.OrderBy( e => e.Key ) );
            }
        }

        [Test]
        public void GetEnumerator()
        {
            var b = CreateSampleTree();
            var e = b.GetEnumerator();
            Assert.AreEqual( b.Count, sampleList.Count );
            for( int i = 0; i < b.Count; ++i )
            {
                var success = e.MoveNext();
                Assert.IsTrue( success, "MoveNext failed." );
                Assert.AreEqual( e.Current, sortedList[i] );
            }
            var isLast = e.MoveNext();
            Assert.IsFalse( isLast, "MoveNext did not indicate end of list." );
        }

        [Test]
        public void CopyTo()
        {
            var b = CreateSampleTree();
            KeyValuePair<int, int>[] array = new KeyValuePair<int, int>[10 + b.Count];
            b.CopyTo( array, 5 );
            var fiveZeros = Enumerable.Repeat( 0, 5 ).Select( i => new KeyValuePair<int, int>( 0, 0 ) );
            fiveZeros.Concat( sortedList ).Concat( fiveZeros ).AssertEqual( array );
        }

        [Test]
        public void RemoveAt()
        {
            var b = CreateSampleTree();
            var cg = b.ToList();
            while( cg.Count > 0 )
            {
                int n = rand.Next( cg.Count );
                b.RemoveAt( n );
                cg.RemoveAt( n );
                b.AssertEqual( cg );
            }
        }

        [Test]
        public void GetUpper()
        {
            var b = CreateSampleTree();
            for( int i = 0; i < sortedList.Count; ++i )
            {
                b.WhereGreaterOrEqual( sortedList[i].Key ).AssertEqual( sortedList.Where( n => n.Key >= sortedList[i].Key ) );
                b.WhereGreaterOrEqual( sortedList[i].Key - 1 ).AssertEqual( sortedList.Where( n => n.Key >= sortedList[i].Key - 1 ) );
            }
            b.WhereGreaterOrEqual( sortedList.Last().Key + 1 ).AssertEqual( Enumerable.Empty<KeyValuePair<int, int>>() );
        }

        [Test]
        public void GetLower()
        {
            var b = CreateSampleTree();
            var r = sortedList.Reverse().ToArray();
            for( int i = 0; i < r.Length; ++i )
            {
                b.WhereLessOrEqualBackwards( r[i].Key ).AssertEqual( r.Where( n => n.Key <= r[i].Key ) );
                b.WhereLessOrEqualBackwards( r[i].Key - 1 ).AssertEqual( r.Where( n => n.Key <= r[i].Key - 1 ) );
            }
        }

        [Test]
        public void Keys()
        {
            var b = CreateSampleTree();
            sortedList.Select( i => i.Key ).AssertEqual( b.Keys );
        }

        [Test]
        public void Values()
        {
            var b = CreateSampleTree();
            sortedList.Select( i => i.Value ).AssertEqual( b.Values );
        }

		[Test]
		public void Range()
		{
			var b = CreateSampleTree();
			var samples = new[]
			{
				new { start = 10, count = 10 },
				new { start = 100, count = 800 }
			};
			foreach( var sample in samples )
			{
				var expected = sortedList.Skip( sample.start ).Take( sample.count ).ToArray();
				Assert.IsTrue( expected.SequenceEqual( b.WhereInRange( expected.First().Key, expected.Last().Key ) ), "first/last" );
				Assert.IsTrue( expected.SequenceEqual( b.WhereInRange( expected.First().Key, expected.Last().Key ) ), "first-1/last+1" );
			}
		}

        #endregion
    }
}
