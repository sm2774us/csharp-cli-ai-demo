// Benchmarks WordStat.WordAnalyzer.Analyze on a generated corpus.
//
// IMPORTANT: WordAnalyzer.Tokenize keeps only alphabetic characters,
// so a vocabulary like "word0".."word9999" would collapse into a
// single token ("word") after tokenization -- silently defeating
// --vocab entirely. This generator instead builds distinct
// alphabetic-only words (bijective base-26, like spreadsheet column
// names: a, b, ..., z, aa, ab, ...), so the requested vocabulary size
// is the actual vocabulary size.
//
// Usage:
//   dotnet run --project scripts/benchmark -- --words 20000 --vocab 15000

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using WordStat;

namespace WordStat.Benchmark;

public static class Program {
  /// <summary>Converts a non-negative index to a unique letter word.</summary>
  /// <param name="index">Zero-based index.</param>
  /// <returns>A unique lowercase string of letters.</returns>
  public static string IndexToWord(int index) {
    var letters = new List<char>();
    var n = index + 1;
    while (n > 0) {
      n -= 1;
      letters.Add((char)('a' + (n % 26)));
      n /= 26;
    }
    letters.Reverse();
    return new string(letters.ToArray());
  }

  /// <summary>Builds a synthetic corpus with a genuinely distinct vocabulary.</summary>
  /// <param name="wordCount">Total number of words to generate.</param>
  /// <param name="vocabSize">Number of distinct words in the vocabulary.</param>
  /// <param name="seed">Random seed for reproducibility.</param>
  /// <returns>A space-separated string of wordCount words.</returns>
  public static string MakeCorpus(int wordCount, int vocabSize, int seed = 42) {
    var rng = new Random(seed);
    var vocab = new string[vocabSize];
    for (var i = 0; i < vocabSize; i++) {
      vocab[i] = IndexToWord(i);
    }

    var sb = new StringBuilder();
    for (var i = 0; i < wordCount; i++) {
      if (i > 0) {
        sb.Append(' ');
      }
      sb.Append(vocab[rng.Next(vocabSize)]);
    }
    return sb.ToString();
  }

  /// <summary>Times a single Analyze() call.</summary>
  /// <param name="wordCount">Corpus size in words.</param>
  /// <param name="vocabSize">Distinct word count.</param>
  /// <param name="topN">Number of top words requested.</param>
  /// <returns>Elapsed wall-clock seconds.</returns>
  public static double Run(int wordCount, int vocabSize, int topN = 10) {
    var text = MakeCorpus(wordCount, vocabSize);
    var stopwatch = Stopwatch.StartNew();
    WordAnalyzer.Analyze(text, topN);
    stopwatch.Stop();
    return stopwatch.Elapsed.TotalSeconds;
  }

  /// <summary>CLI entry point for the benchmark.</summary>
  /// <param name="args">Command-line arguments: --words N --vocab N.</param>
  public static void Main(string[] args) {
    var words = 20000;
    var vocab = 15000;

    for (var i = 0; i < args.Length; i++) {
      if (args[i] == "--words" && i + 1 < args.Length) {
        words = int.Parse(args[i + 1]);
        i++;
      } else if (args[i] == "--vocab" && i + 1 < args.Length) {
        vocab = int.Parse(args[i + 1]);
        i++;
      }
    }

    var elapsed = Run(words, vocab);
    Console.WriteLine(
        $"words={words} vocab={vocab} elapsed_seconds={elapsed:F4}");
  }
}

