using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLib.Collections;

namespace VLib.Tests
{
    public class CompoundArrayViewTests
    {
        [Test]
        public void BasicIterationWithOffsetsMatches()
        {
            var chunks = new int[][]
            {
                new[] { 0, 1, 2, 3, 4, 5 },
                new[] { 6, 7, 8, 9, 10, 11 },
                new[] { 12, 13, 14, 15, 16, 17 }
            };

            const int startIndex = 2;
            const int endOffset = 3;
            var expected = new List<int>();
            for (int i = startIndex; i < chunks[0].Length; i++)
                expected.Add(chunks[0][i]);
            for (int i = 0; i < chunks[1].Length; i++)
                expected.Add(chunks[1][i]);
            for (int i = 0; i < chunks[2].Length - endOffset; i++)
                expected.Add(chunks[2][i]);

            var view = new CompoundArrayView<int>(chunks, startIndex, endOffset);

            Assert.IsTrue(expected.Count == view.Count);

            var forResults = new List<int>();
            for (int i = 0; i < view.Count; i++)
                forResults.Add(view[i]);

            var foreachResults = new List<int>();
            foreach (var value in view)
                foreachResults.Add(value);

            Assert.IsTrue(expected.Count == forResults.Count);
            Assert.IsTrue(expected.Count == foreachResults.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.IsTrue(expected[i] == forResults[i], $"For loop value mismatch at index {i}");
                Assert.IsTrue(expected[i] == foreachResults[i], $"Foreach value mismatch at index {i}");
            }
        }

        [Test]
        public void MultipleChunksNoOffsetsMatches()
        {
            var chunks = new int[][]
            {
                new[] { 10, 20, 30 },
                new[] { 40, 50 },
                new[] { 60, 70, 80, 90 },
                new[] { 100 }
            };

            var view = new CompoundArrayView<int>(chunks, 0, 0);
            var expected = chunks.SelectMany(c => c).ToArray();

            Assert.IsTrue(expected.Length == view.Count);

            for (int i = 0; i < view.Count; i++)
                Assert.IsTrue(expected[i] == view[i], $"Indexer mismatch at {i}");

            int index = 0;
            foreach (var value in view)
            {
                Assert.IsTrue(expected[index] == value, $"Foreach mismatch at {index}");
                index++;
            }

            Assert.IsTrue(expected.Length == index, "Foreach didn't enumerate all elements");
        }

        [Test]
        public void SingleChunkWithOffsetsMatches()
        {
            var chunks = new int[][]
            {
                new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }
            };

            const int startIndex = 3;
            const int endOffset = 2;
            var view = new CompoundArrayView<int>(chunks, startIndex, endOffset);

            var expected = new[] { 3, 4, 5, 6, 7 };

            Assert.IsTrue(expected.Length == view.Count);

            for (int i = 0; i < view.Count; i++)
                Assert.IsTrue(expected[i] == view[i]);

            var foreachResults = new List<int>();
            foreach (var value in view)
                foreachResults.Add(value);

            Assert.IsTrue(expected.SequenceEqual(foreachResults));
        }

        [Test]
        public void RandomChunksWithRandomOffsetsMatches()
        {
            var chunkCount = Random.Range(2, 8);
            var chunks = new int[chunkCount][];
            var currentValue = 0;

            for (int c = 0; c < chunkCount; c++)
            {
                var chunkSize = Random.Range(3, 15);
                chunks[c] = new int[chunkSize];
                for (int i = 0; i < chunkSize; i++)
                {
                    chunks[c][i] = currentValue;
                    currentValue++;
                }
            }

            var startIndex = Random.Range(0, chunks[0].Length - 1);
            var endOffset = Random.Range(0, chunks[chunkCount - 1].Length - 1);

            var view = new CompoundArrayView<int>(chunks, startIndex, endOffset);

            var expected = new List<int>();
            for (int c = 0; c < chunkCount; c++)
            {
                var startIdx = (c == 0) ? startIndex : 0;
                var endIdx = (c == chunkCount - 1) ? chunks[c].Length - endOffset : chunks[c].Length;
                for (int i = startIdx; i < endIdx; i++)
                    expected.Add(chunks[c][i]);
            }

            Assert.IsTrue(expected.Count == view.Count);

            for (int i = 0; i < view.Count; i++)
                Assert.IsTrue(expected[i] == view[i], $"Random test indexer mismatch at {i}");

            var foreachResults = view.ToList();
            Assert.IsTrue(expected.SequenceEqual(foreachResults), "Random test foreach mismatch");
        }

        [Test]
        public void EnumeratorResetWorks()
        {
            var chunks = new int[][]
            {
                new[] { 1, 2, 3 },
                new[] { 4, 5, 6 }
            };

            var view = new CompoundArrayView<int>(chunks, 0, 0);
            var enumerator = view.GetEnumerator();

            var firstPass = new List<int>();
            while (enumerator.MoveNext())
                firstPass.Add(enumerator.Current);

            enumerator.Reset();
            var secondPass = new List<int>();
            while (enumerator.MoveNext())
                secondPass.Add(enumerator.Current);

            Assert.IsTrue(firstPass.SequenceEqual(secondPass), "Enumerator reset failed");
        }

        [Test]
        public void ElementAtReadOnlyReturnsCorrectReference()
        {
            var chunks = new int[][]
            {
                new[] { 10, 20, 30 },
                new[] { 40, 50, 60 }
            };

            var view = new CompoundArrayView<int>(chunks, 1, 1);

            ref readonly var first = ref view.ElementAtReadOnly(0);
            ref readonly var second = ref view.ElementAtReadOnly(1);
            ref readonly var third = ref view.ElementAtReadOnly(2);
            ref readonly var fourth = ref view.ElementAtReadOnly(3);

            Assert.IsTrue(20 == first);
            Assert.IsTrue(30 == second);
            Assert.IsTrue(40 == third);
            Assert.IsTrue(50 == fourth);
        }

        // Exception tests unchanged

        [Test]
        public void ThrowsOnNullChunksArray()
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                var view = new CompoundArrayView<int>(null, 0, 0);
            });
        }

        [Test]
        public void ThrowsOnEmptyChunksArray()
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                var view = new CompoundArrayView<int>(new int[0][], 0, 0);
            });
        }

        [Test]
        public void ThrowsOnNullChunk()
        {
            var chunks = new int[][]
            {
                new[] { 1, 2, 3 },
                null,
                new[] { 4, 5, 6 }
            };

            Assert.Throws<System.ArgumentException>(() =>
            {
                var view = new CompoundArrayView<int>(chunks, 0, 0);
            });
        }

        [Test]
        public void ThrowsOnEmptyChunk()
        {
            var chunks = new int[][]
            {
                new[] { 1, 2, 3 },
                new int[0],
                new[] { 4, 5, 6 }
            };

            Assert.Throws<System.ArgumentException>(() =>
            {
                var view = new CompoundArrayView<int>(chunks, 0, 0);
            });
        }

        [Test]
        public void ThrowsOnNegativeStartIndex()
        {
            var chunks = new int[][] { new[] { 1, 2, 3 } };

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                var view = new CompoundArrayView<int>(chunks, -1, 0);
            });
        }

        [Test]
        public void ThrowsOnNegativeEndOffset()
        {
            var chunks = new int[][] { new[] { 1, 2, 3 } };

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                var view = new CompoundArrayView<int>(chunks, 0, -1);
            });
        }

        [Test]
        public void ThrowsOnStartIndexBeyondFirstChunk()
        {
            var chunks = new int[][] { new[] { 1, 2, 3 } };

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                var view = new CompoundArrayView<int>(chunks, 5, 0);
            });
        }

        [Test]
        public void ThrowsOnEndOffsetBeyondLastChunk()
        {
            var chunks = new int[][] { new[] { 1, 2, 3 } };

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                var view = new CompoundArrayView<int>(chunks, 0, 5);
            });
        }

        [Test]
        public void ThrowsOnIndexOutOfRange()
        {
            var chunks = new int[][] { new[] { 1, 2, 3, 4, 5 } };
            var view = new CompoundArrayView<int>(chunks, 1, 1);

            Assert.DoesNotThrow(() => { var _ = view[0]; });
            Assert.DoesNotThrow(() => { var _ = view[2]; });

            Assert.Throws<System.IndexOutOfRangeException>(() =>
            {
                var _ = view[3];
            });

            Assert.Throws<System.IndexOutOfRangeException>(() =>
            {
                var _ = view[-1];
            });
        }

        [Test]
        public void IReadOnlyListInterfaceWorks()
        {
            var chunks = new int[][]
            {
                new[] { 1, 2, 3 },
                new[] { 4, 5, 6 }
            };

            IReadOnlyList<int> view = new CompoundArrayView<int>(chunks, 0, 0);

            Assert.IsTrue(6 == view.Count);
            Assert.IsTrue(1 == view[0]);
            Assert.IsTrue(6 == view[5]);

            var enumerated = new List<int>();
            foreach (var value in view)
                enumerated.Add(value);

            Assert.IsTrue(enumerated.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 }));
        }

        [Test]
        public void LargeDataSetStressTest()
        {
            const int chunkCount = 100;
            var chunks = new int[chunkCount][];
            var currentValue = 0;

            for (int c = 0; c < chunkCount; c++)
            {
                var chunkSize = Random.Range(10, 50);
                chunks[c] = new int[chunkSize];
                for (int i = 0; i < chunkSize; i++)
                {
                    chunks[c][i] = currentValue;
                    currentValue++;
                }
            }

            const int startIndex = 5;
            const int endOffset = 7;
            var view = new CompoundArrayView<int>(chunks, startIndex, endOffset);

            var previousValue = startIndex - 1;
            foreach (var value in view)
            {
                Assert.IsTrue(previousValue + 1 == value, "Sequential value check failed");
                previousValue = value;
            }

            int idx = 0;
            foreach (var value in view)
            {
                Assert.IsTrue(value == view[idx], $"Indexer mismatch at {idx}");
                idx++;
            }
        }
    }
}