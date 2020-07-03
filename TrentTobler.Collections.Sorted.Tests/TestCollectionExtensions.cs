// Copyright 2011-2020 Trent Tobler.All rights reserved.
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
using System.Linq;
using NUnit.Framework;

namespace TrentTobler.Collections.Sorted.Tests
{
    static class TestCollectionExtensions
    {
        public static readonly int seed = new Random().Next();
        static Random rand = new Random( seed );

        public static void ForEach<T>( this IEnumerable<T> list, Action<T> action )
        {
            foreach( var item in list )
                action( item );
        }

        public static IEnumerable<T> Choose<T>( this IEnumerable<T> source, int count, int totalCount )
        {
            var i = source.GetEnumerator();
            while( count > 0 && i.MoveNext() )
            {
                if( rand.Next( totalCount ) < count )
                {
                    yield return i.Current;
                    --count;
                }
                --totalCount;
            }
        }

        public static IEnumerable<T> Shuffle<T>( this IEnumerable<T> source )
        {
            List<T> list = new List<T>( source );
            for( int i = 0; i < list.Count; ++i )
            {
                int n = rand.Next( list.Count - i ) + i;
                var t = list[n];
                list[n] = list[i];
                list[n] = t;
            }
            return list;
        }

        public static void AddRange<T>( this ICollection<T> collection, IEnumerable<T> items )
        {
            foreach( var item in items )
                collection.Add( item );
        }

        public static void AssertEqual<T>( this IEnumerable<T> left, IEnumerable<T> right )
        {
            if( left.SequenceEqual( right ) )
                return;

            string message;

            var lc = left.Count();
            var rc = right.Count();
            if( left.Count() != right.Count() )
            {
                message = string.Format( "sequences differ in length: {0} <> {1}.  ({2}..{3}):({4}..{5})",
                    lc,
                    rc,
                    string.Join( ",", left.Take( 10 ) ),
                    string.Join( ",", left.Skip( lc - 3 ) ),
                    string.Join( ",", right.Take( 10 ) ),
                    string.Join( ",", right.Skip( rc - 3 ) ) );
            }
            else
            {

                var diffIndex = left.Zip( right, ( l, r ) => new
                {
                    Left = l,
                    Right = r,
                } ).TakeWhile( e => object.Equals( e.Left, e.Right ) ).Count();

                message = string.Format( "sequences differ at [{0}]: {1} <> {2}", diffIndex, left.ElementAt( diffIndex ), right.ElementAt( diffIndex ) );
            }
            Assert.Fail( message );
        }
    }
}
