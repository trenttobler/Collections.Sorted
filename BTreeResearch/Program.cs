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
using System.Text.RegularExpressions;
using TrentTobler.Collections;

namespace BSkipTreeResearch
{
    class Program
    {
        struct OptionCommand
        {
            public string Text
            {
                get;
                set;
            }

            public Action Action
            {
                get;
                set;
            }
        }

        static int nodeCapacity = 128;

        static void Main( string[] args )
        {
            var rlist = Enumerable.Range( 0, RandList.Count )
                .Select( i =>
                    new KeyValuePair<int, int>( RandList[i], i ) );
            var olist = Enumerable.Range( 0, RandList.Count )
                .Select( i =>
                    new KeyValuePair<int, int>( i, i ) );
            var revlist = Enumerable.Range( 0, RandList.Count )
                .Select( i =>
                    new KeyValuePair<int, int>( RandList.Count - i - 1, i ) );

            var sourceList = rlist;

            while( true )
            {
                Console.Write( @"
A) List
B) Dictionary
C) SortedDictionary
D) BTreeDictionary
F) BTree

N) Run NUnit tests using reflection.

R) use random source
O) use ordered source
P) use reverse order source
=n) use n as the btree node capacity.

Enter one or more options: " );

                var optionText = Console.ReadLine();
                if( string.IsNullOrEmpty( optionText ) )
                    break;

                var options = Regex.Matches( optionText, "[a-zA-Z]|(=[0-9]+)" ).OfType<Match>().Select( m => m.Value ).ToArray();

                Console.WriteLine( "Executing..." );

                foreach( var option in options )
                {
                    if( option.StartsWith( "=" ) )
                    {
                        int n;
                        if( int.TryParse( option.Substring( 1 ), out n ) )
                        {
                            nodeCapacity = n;
                            Console.WriteLine( "nodeCapacity:{0}", n );
                        }
                        continue;
                    }

                    switch( option )
                    {
                        case "N":
                            ExecuteNUnitTests();
                            break;

                        case "R":
                            sourceList = rlist;
                            break;
                        case "O":
                            sourceList = olist;
                            break;
                        case "P":
                            sourceList = revlist;
                            break;

                        case "A":
                            CollectionFull_Test( new List<KeyValuePair<int, int>>(), sourceList );
                            break;
                        case "B":
                            CollectionFull_Test( new Dictionary<int, int>(), sourceList );
                            break;
                        case "C":
                            CollectionFull_Test( new SortedDictionary<int, int>(), sourceList );
                            break;
                        case "D":
                            CollectionFull_Test( new BTreeDictionary<int, int>( nodeCapacity ), sourceList );
                            break;
                        case "F":
                            CollectionFull_Test( new BTree<int>( nodeCapacity ), sourceList.Select( item => item.Key ) );
                            break;
                    }
                }
            }
        }

        private static void ExecuteNUnitTests()
        {
            var tests =
                from t in typeof( TrentTobler.Collections.Sorted.Tests.BTreeTests ).Assembly.GetTypes()
                from m in t.GetMethods()
                where m.ReturnType.Name == "Void"
                where m.GetCustomAttributes( false ).Any( a => a.GetType().Name == "TestAttribute" )
                let p = m.GetParameters()
                where p == null || p.Length == 0
                select m;
            foreach( var testMethod in tests )
            {
                var testInstance = Activator.CreateInstance( testMethod.DeclaringType );
                testMethod.Invoke( testInstance, null );
            }
        }

        static void CollectionFull_Test<T>( ICollection<T> collection, IEnumerable<T> items )
        {
            var time = TimeAction( () =>
            {
                foreach( var item in items )
                    collection.Add( item );
                foreach( var item in items )
                    collection.Remove( item );
            } );

            Console.WriteLine(
                "{0} = {1}",
                time.TotalMilliseconds.ToString( "0.000" ).PadLeft( 8 ),
                collection.GetType().Name );
        }

        static void CollectionAdd_Test<T>( ICollection<T> collection, IEnumerable<T> items )
        {
            var time = TimeAction( () =>
            {
                foreach( var item in items )
                    collection.Add( item );
            } );

            Console.WriteLine(
                "{0} = {1}",
                time.TotalMilliseconds.ToString( "0.000" ).PadLeft( 8 ),
                collection.GetType().Name );
        }

        static List<int> CreateRandList( int count )
        {
            var list = new List<int>();
            for( int i = 0; i < count; ++i )
            {
                list.Add( i );
                int r = rand.Next( list.Count );
                int n = list[r];
                list[r] = list[list.Count - 1];
                list[list.Count - 1] = n;
            }
            return list;
        }

        const int testCount = 1000000;
        static Random rand = new Random( 101 );
        static List<int> randList = null;
        static List<int> RandList
        {
            get
            {
                if( randList == null )
                {
                    randList = CreateRandList( testCount );
                }
                return randList;
            }
        }

        static TimeSpan TimeAction( Action a )
        {
            DateTime start = DateTime.Now.ToUniversalTime();
            a();
            DateTime done = DateTime.Now.ToUniversalTime();
            return done - start;
        }
    }
}
