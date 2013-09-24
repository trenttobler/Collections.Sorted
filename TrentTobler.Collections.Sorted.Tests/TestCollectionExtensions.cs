//Copyright 2011 Trent Tobler. All rights reserved.

//Redistribution and use in source and binary forms, with or without modification, are
//permitted provided that the following conditions are met:

//   1. Redistributions of source code must retain the above copyright notice, this list of
//      conditions and the following disclaimer.

//   2. Redistributions in binary form must reproduce the above copyright notice, this list
//      of conditions and the following disclaimer in the documentation and/or other materials
//      provided with the distribution.

//THIS SOFTWARE IS PROVIDED BY TRENT TOBLER ''AS IS'' AND ANY EXPRESS OR IMPLIED
//WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
//FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL TRENT TOBLER OR
//CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
//CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
//ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
//ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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
