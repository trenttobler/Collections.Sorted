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
N=n) use n as the btree node capacity (current: {nodeCapacity})

Enter one or more options: " );

				var optionText = Console.ReadLine();
				if( string.IsNullOrEmpty( optionText ) )
					break;

				var options = Regex.Matches( optionText, "(?<cmd>[a-zA-Z])(=(?<arg>[0-9]+))?" ).OfType<Match>().ToArray();
				Console.WriteLine( "Executing..." );

				bool ParseArg( Match option, out int n ) => int.TryParse( option.Groups["arg"].Value, out n );

				foreach( var option in options )
				{
					var cmd = option.Groups["cmd"].Value;

					switch( cmd )
					{
						case "N":
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
							TestCollection( new List<KeyValuePair<int, int>>(), GetSourceList( sourceListOrder ) );
							break;

						case "B":
							TestCollection( new Dictionary<int, int>(), GetSourceList( sourceListOrder ) );
							break;

						case "C":
							TestCollection( new SortedDictionary<int, int>(), GetSourceList( sourceListOrder ) );
							break;

						case "D":
							TestCollection( new BTreeDictionary<int, int>( nodeCapacity ), GetSourceList( sourceListOrder ) );
							break;

						case "F":
							TestCollection( new BTree<int>( nodeCapacity ), GetSourceList( sourceListOrder ).Select( item => item.Key ) );
							break;
					}
				}
			}
		}

		static void TestCollection<T>( ICollection<T> collection, IEnumerable<T> items )
		{
			CollectionAdd_Test( collection, items );
			CollectionContains_Test( collection, items );

			collection.Clear();
			CollectionFull_Test( collection, items );
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
			Console.WriteLine( $"{ms} = {collection.GetType().Name} [add + remove]" );
		}

		static void CollectionAdd_Test<T>( ICollection<T> collection, IEnumerable<T> items )
		{
			var time = TimeAction( () =>
			{
				foreach( var item in items )
					collection.Add( item );
			} );

			var addMs = time.TotalMilliseconds.ToString( "0.000" ).PadLeft( 8 );
			Console.WriteLine( $"{addMs} = {collection.GetType().Name} [add]" );
		}

		static void CollectionContains_Test<T>( ICollection<T> collection, IEnumerable<T> items )
		{
			var found = 0;

			var time = TimeAction( () =>
			{
				foreach( var item in items )
					if( collection.Contains( item ) )
						++found;
			} );

			var addMs = time.TotalMilliseconds.ToString( "0.000" ).PadLeft( 8 );
			Console.WriteLine( $"{addMs} = {collection.GetType().Name} [contains = {found}]" );
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
