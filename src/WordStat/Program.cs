// wordstat: a word-frequency CLI tool.
//
// This file is INTENTIONALLY sub-optimal. It exists as a teaching
// example: an AI coding agent (Claude Code, Codex, Copilot, Gemini)
// is meant to read this, find the algorithmic and style issues, and
// produce an improved version with benchmarks proving the
// improvement.
//
// Known issues (do not fix here; this is the "before" state):
//   - TopNWords is O(n^2) via repeated linear scans for the max.
//   - Tokenize rebuilds a string one character at a time.
//   - CountWords uses a list with linear search instead of a
//     Dictionary, making it O(n^2) overall.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WordStat;

/// <summary>Word-frequency analysis, deliberately sub-optimal.</summary>
public static class WordAnalyzer {
  /// <summary>Splits text into lowercase alphabetic words.</summary>
  /// <param name="text">Raw input text.</param>
  /// <returns>A list of lowercase word tokens.</returns>
  public static List<string> Tokenize(string text) {
    var words = new List<string>();
    var current = new StringBuilder();
    foreach (char ch in text) {
      if (char.IsLetter(ch)) {
        // Sub-optimal: string concatenation per character via
        // StringBuilder.Append still O(1) amortized, but this loop
        // mirrors the Python "before" state's per-char handling
        // rather than a single regex pass.
        current.Append(char.ToLowerInvariant(ch));
      } else {
        if (current.Length > 0) {
          words.Add(current.ToString());
          current.Clear();
        }
      }
    }
    if (current.Length > 0) {
      words.Add(current.ToString());
    }
    return words;
  }

  /// <summary>Counts word frequencies using linear-search lookup.</summary>
  /// <param name="words">Tokenized words.</param>
  /// <returns>Pairs of (word, count), order of first appearance.</returns>
  public static List<WordCount> CountWords(List<string> words) {
    var counts = new List<WordCount>();
    foreach (var word in words) {
      var found = false;
      for (var i = 0; i < counts.Count; i++) {
        if (counts[i].Word == word) {
          counts[i] = new WordCount(counts[i].Word, counts[i].Count + 1);
          found = true;
          break;
        }
      }
      if (!found) {
        counts.Add(new WordCount(word, 1));
      }
    }
    return counts;
  }

  /// <summary>Returns the top-n counts by frequency, descending.</summary>
  /// <param name="counts">List of word counts.</param>
  /// <param name="n">Number of top entries to return.</param>
  /// <returns>Up to n entries sorted by count descending.</returns>
  public static List<WordCount> TopNWords(List<WordCount> counts, int n) {
    var remaining = new List<WordCount>(counts);
    var result = new List<WordCount>();
    var take = Math.Min(n, remaining.Count);
    for (var i = 0; i < take; i++) {
      var bestIndex = 0;
      for (var j = 1; j < remaining.Count; j++) {
        if (remaining[j].Count > remaining[bestIndex].Count) {
          bestIndex = j;
        }
      }
      result.Add(remaining[bestIndex]);
      remaining.RemoveAt(bestIndex);
    }
    return result;
  }

  /// <summary>Runs the full analysis pipeline on a text blob.</summary>
  /// <param name="text">Raw input text.</param>
  /// <param name="topN">Number of top words to return.</param>
  /// <returns>Top-n word counts.</returns>
  public static List<WordCount> Analyze(string text, int topN) {
    var words = Tokenize(text);
    var counts = CountWords(words);
    return TopNWords(counts, topN);
  }
}

/// <summary>A single word and its occurrence count.</summary>
public readonly struct WordCount {
  public WordCount(string word, int count) {
    Word = word;
    Count = count;
  }

  public string Word { get; }

  public int Count { get; }
}

/// <summary>CLI entry point for wordstat.</summary>
public static class Program {
  /// <summary>Parsed command-line arguments.</summary>
  public readonly struct ParsedArgs {
    public ParsedArgs(string filePath, int topN) {
      FilePath = filePath;
      TopN = topN;
    }

    public string FilePath { get; }

    public int TopN { get; }
  }

  /// <summary>Parses CLI arguments in the form: FILE [-n|--top N].</summary>
  /// <param name="args">Raw command-line arguments.</param>
  /// <returns>Parsed file path and top-n count.</returns>
  /// <exception cref="ArgumentException">
  /// Thrown when arguments are missing or malformed.
  /// </exception>
  public static ParsedArgs ParseArgs(string[] args) {
    string? filePath = null;
    var topN = 10;

    for (var i = 0; i < args.Length; i++) {
      var arg = args[i];
      if (arg == "-n" || arg == "--top") {
        if (i + 1 >= args.Length) {
          throw new ArgumentException($"missing value for {arg}");
        }
        if (!int.TryParse(args[i + 1], out topN)) {
          throw new ArgumentException($"invalid integer for {arg}: {args[i + 1]}");
        }
        i++;
      } else if (filePath is null) {
        filePath = arg;
      } else {
        throw new ArgumentException($"unexpected argument: {arg}");
      }
    }

    if (filePath is null) {
      throw new ArgumentException("missing required argument: file");
    }

    return new ParsedArgs(filePath, topN);
  }

  /// <summary>CLI entry point.</summary>
  /// <param name="args">Command-line arguments.</param>
  /// <returns>Process exit code (0 on success, 1 on error).</returns>
  public static int Main(string[] args) {
    ParsedArgs parsed;
    try {
      parsed = ParseArgs(args);
    } catch (ArgumentException ex) {
      Console.Error.WriteLine($"error: {ex.Message}");
      return 1;
    }

    if (!File.Exists(parsed.FilePath)) {
      Console.Error.WriteLine($"error: file not found: {parsed.FilePath}");
      return 1;
    }

    var text = File.ReadAllText(parsed.FilePath);
    var results = WordAnalyzer.Analyze(text, parsed.TopN);

    foreach (var entry in results) {
      Console.WriteLine($"{entry.Word}\t{entry.Count}");
    }

    return 0;
  }
}

