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

        static void Main( string[] args )
        {
			var seed = 101;
			var sampleCount = 10000;
			var nodeCapacity = 128;

			var randList = CreateRandList( seed, sampleCount );

			IEnumerable<KeyValuePair<int, int>> GetSourceList( char order )
			{
				switch( char.ToUpper( order ) )
				{
					case 'R': return randList.Select( ( v, i ) => new KeyValuePair<int, int>( v, i ) );
					case 'O': return randList.Select( ( v, i ) => new KeyValuePair<int, int>( i, i ) );
					case 'P': return randList.Select( ( v, i ) => new KeyValuePair<int, int>( randList.Count - i - 1, i ) );
					default: throw new NotImplementedException( $"Order {order}" );
				};
			}

            var sourceListOrder = 'R';

            while( true )
            {
                Console.Write( $@"
A) List
B) Dictionary
C) SortedDictionary
D) BTreeDictionary
F) BTree

R) use random source
O) use ordered source
P) use reverse order source

S=n) set seed to n (current: {seed})
L=n) set length of sample data to n (current: {sampleCount})
=n) use n as the btree node capacity (current: {nodeCapacity})

Enter one or more options: " );

                var optionText = Console.ReadLine();
                if( string.IsNullOrEmpty( optionText ) )
                    break;

				var options = Regex.Matches( optionText, "(?<cmd>[a-zA-Z])?(=(?<arg>[0-9]+))?" ).OfType<Match>().ToArray();
                Console.WriteLine( "Executing..." );

				bool ParseArg( Match option, out int n ) => int.TryParse( option.Groups["arg"].Value, out n );

				foreach( var option in options )
                {
					var cmd = option.Groups["cmd"].Value;

					switch( cmd )
					{
						case "":
							if( ParseArg( option, out var newNodeCapacity ) )
							{
								nodeCapacity = newNodeCapacity;
								Console.WriteLine( "nodeCapacity:{0}", nodeCapacity );
							}
							break;

						case "S":
							if( ParseArg( option, out var newSeed ) )
							{
								seed = newSeed;
								randList = CreateRandList( seed, sampleCount );
								Console.WriteLine( "seed:{0}", seed );
							}
							break;

						case "L":
							if( ParseArg( option, out var newSampleCount ) )
							{
								sampleCount = newSampleCount;
								randList = CreateRandList( seed, sampleCount );
								Console.WriteLine( "sample count:{0}", sampleCount );
							}
							break;

						case "R":
						case "O":
						case "P":
							sourceListOrder = cmd[0];
                            break;

                        case "A":
                            CollectionFull_Test( new List<KeyValuePair<int, int>>(), GetSourceList( sourceListOrder ) );
                            break;

                        case "B":
                            CollectionFull_Test( new Dictionary<int, int>(), GetSourceList( sourceListOrder ) );
                            break;

                        case "C":
                            CollectionFull_Test( new SortedDictionary<int, int>(), GetSourceList( sourceListOrder ) );
                            break;

                        case "D":
                            CollectionFull_Test( new BTreeDictionary<int, int>( nodeCapacity ), GetSourceList( sourceListOrder ) );
                            break;

                        case "F":
                            CollectionFull_Test( new BTree<int>( nodeCapacity ), GetSourceList( sourceListOrder ).Select( item => item.Key ) );
                            break;
                    }
                }
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

			var ms = time.TotalMilliseconds.ToString( "0.000" ).PadLeft( 8 );
			Console.WriteLine( $"{ms} = {collection.GetType().Name} [{items.Count()}]" );
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

        static List<int> CreateRandList( int seed, int count )
        {
			Random rand = new Random( seed );

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

        static TimeSpan TimeAction( Action a )
        {
			var stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();
            a();
			stopwatch.Stop();
            return stopwatch.Elapsed;
        }
    }
}
