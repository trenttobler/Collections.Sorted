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
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;

namespace TrentTobler.Collections.Sorted.Tests
{
    [TestFixture]
    public class BTreeTests
    {
        #region Implementation - Helpers

        static Random rand = new Random( 101 );

        static ReadOnlyCollection<int> sampleList = new ReadOnlyCollection<int>( 
            Enumerable.Range( 0, 1000 )
            .Select( i => i * 10 )
            .Shuffle()
            .ToArray() );
        
        static ReadOnlyCollection<int> sortedList = new ReadOnlyCollection<int>( 
            sampleList
            .OrderBy( i => i )
            .ToArray() );

        static int[] CreateRandomHalf()
        {
            var half = sampleList.Choose( sampleList.Count / 2, sampleList.Count ).ToArray();
            Assert.IsNotEmpty( half );
            return half;
        }

        const int testNodeCapacity = 10;

        static BTree<int> CreateSampleTree()
        {
            BTree<int> b = new BTree<int>( testNodeCapacity );
            b.AddRange( sampleList );
            return b;
        }

        #endregion

        #region Tests

        [Test]
        public void AddRemoveFirstKeyBugTest()
        {
            // CodeProject.com comment / comment with bug sequence (Thanks, Member 11028508!)
            var btree = new BTree<int>( 3 );
            btree.Add( 1 );
            btree.Add( 2 );
            btree.Add( 3 );
            btree.Add( 4 );
            btree.Add( 5 );
            btree.Add( 6 );
            btree.Add( 7 );
            btree.Add( 8 );
            btree.Add( 9 );

            btree.Remove( 3 );
            btree.RemoveAt( 0 );

            Assert.AreEqual( "2,4,5,6,7,8,9", string.Join( ",", btree ) );
        }

        [Test]
        public void AllowDuplicates()
        {
            var duplicates = new List<int>();
            for( int i = 0; i < 10000; ++i )
                duplicates.Add( rand.Next( 1000 ) );

            var orderedDuplicates = new List<int>( duplicates );
            orderedDuplicates.Sort();

            var b = new BTree<int>( testNodeCapacity )
            {
                AllowDuplicates = true,
            };

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
                if( prev != here )
                {
                    Assert.AreEqual( i, b.FirstIndexWhereGreaterThan( prev ) );
                    Assert.AreEqual( i - 1, b.LastIndexWhereLessThan( here ) );
                    Assert.IsTrue( b.Contains( prev ) );
                    Assert.IsTrue( b.Contains( here ) );

                    Assert.AreEqual( here, b.WhereGreaterOrEqual( here ).First() );
                    Assert.AreEqual( here, b.WhereLessOrEqualBackwards( here ).First() );
                    b.WhereGreaterOrEqual( here ).AssertEqual( orderedDuplicates.Skip( i ) );
                    b.WhereLessOrEqualBackwards( prev ).AssertEqual( orderedDuplicates.Take( i ).Reverse() );
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
        public void Contains()
        {
            var b = CreateSampleTree();
            sampleList.Where( i => !b.Contains( i ) ).AssertEqual( Enumerable.Empty<int>() );
            sampleList.Where( i => b.Contains( i + 1 ) ).AssertEqual( Enumerable.Empty<int>() );
        }

        [Test]
        public void Remove()
        {
            var b = CreateSampleTree();
            var half = CreateRandomHalf();
            half.ForEach( i => b.Remove( i ) );
            sortedList.Where( i => !half.Contains( i ) ).AssertEqual( b );
        }

        [Test]
        public void Clear()
        {
            var b = CreateSampleTree();

            for( int i = 0; i < 10; ++i )
            {
                b.Clear();
                b.AssertEqual( Enumerable.Empty<int>() );

                var s = CreateRandomHalf();
                b.AddRange( s );
                b.AssertEqual( s.OrderBy( e => e ) );
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
            int[] array = new int[10+b.Count];
            b.CopyTo( array, 5 );
            var fiveZeros = Enumerable.Repeat( 0, 5 );
            fiveZeros.Concat( sortedList ).Concat( fiveZeros ).AssertEqual( array );
        }

        [Test]
        public void FirstIndexWhereGreaterThan()
        {
            var b = CreateSampleTree();
            for( int i = 0; i < sortedList.Count; ++i )
            {
                Assert.AreEqual( i, b.FirstIndexWhereGreaterThan( sortedList[i] - 1 ), "wrong index returned (existing key)." );
                Assert.AreEqual( i + 1, b.FirstIndexWhereGreaterThan( sortedList[i] ), "wrong index returned (existing key)." );
                Assert.AreEqual( i + 1, b.FirstIndexWhereGreaterThan( sortedList[i] + 1 ), "wrong index returned (existing key)." );
            }
        }

        [Test]
        public void LastIndexWhereLessThan()
        {
            var b = CreateSampleTree();
            for( int i = 0; i < sortedList.Count; ++i )
            {
                Assert.AreEqual( i - 1, b.LastIndexWhereLessThan( sortedList[i] - 1 ), "wrong index returned (existing key)." );
                Assert.AreEqual( i - 1, b.LastIndexWhereLessThan( sortedList[i] ), "wrong index returned (existing key)." );
                Assert.AreEqual( i, b.LastIndexWhereLessThan( sortedList[i] + 1 ), "wrong index returned (existing key)." );
            }
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
        public void WhereGreaterOrEqual()
        {
            var b = CreateSampleTree();
            for( int i = 0; i < sortedList.Count; ++i )
            {
                b.WhereGreaterOrEqual( sortedList[i] ).AssertEqual( sortedList.Where( n => n >= sortedList[i] ) );
                b.WhereGreaterOrEqual( sortedList[i] - 1 ).AssertEqual( sortedList.Where( n => n >= sortedList[i] - 1 ) );
            }
            b.WhereGreaterOrEqual( sortedList.Last() + 1 ).AssertEqual( Enumerable.Empty<int>() );
        }

        [Test]
        public void WhereLessOrEqualBackwards()
        {
            var b = CreateSampleTree();
            var r = sortedList.Reverse().ToArray();
            for( int i = 0; i < r.Length; ++i )
            {
                b.WhereLessOrEqualBackwards( r[i] ).AssertEqual( r.Where( n => n <= r[i] ) );
                b.WhereLessOrEqualBackwards( r[i] - 1 ).AssertEqual( r.Where( n => n <= r[i] - 1) );
            }
        }

        [Test]
        public void ForwardFromIndex()
        {
            var b = CreateSampleTree();
            var index = sortedList.Count / 3;
            var expected = sortedList.Skip( index );
            b.ForwardFromIndex( index ).AssertEqual( expected );
        }

        [Test]
        public void BackwardFromIndex()
        {
            var b = CreateSampleTree();
            var index = sortedList.Count / 3;
            var expected = sortedList.Take( index + 1 ).Reverse();
            b.BackwardFromIndex( index ).AssertEqual( expected );
        }

        #endregion
    }
}
