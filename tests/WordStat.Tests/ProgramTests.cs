using System;
using System.Collections.Generic;
using System.IO;
using WordStat;
using Xunit;

namespace WordStat.Tests;

public class WordAnalyzerTests {
  [Fact]
  public void Tokenize_BasicText_ReturnsLowercaseWords() {
    var result = WordAnalyzer.Tokenize("Hello, world! Hello again.");
    Assert.Equal(
        new List<string> { "hello", "world", "hello", "again" }, result);
  }

  [Fact]
  public void Tokenize_EmptyString_ReturnsEmptyList() {
    Assert.Empty(WordAnalyzer.Tokenize(string.Empty));
  }

  [Fact]
  public void Tokenize_NoTrailingDelimiter_ReturnsWord() {
    Assert.Equal(new List<string> { "abc" }, WordAnalyzer.Tokenize("abc"));
  }

  [Fact]
  public void Tokenize_OnlyDelimiters_ReturnsEmptyList() {
    Assert.Empty(WordAnalyzer.Tokenize("!!! ,,, ..."));
  }

  [Fact]
  public void CountWords_Basic_CountsCorrectly() {
    var words = new List<string> { "a", "b", "a", "c", "b", "a" };
    var counts = WordAnalyzer.CountWords(words);
    var asDict = new Dictionary<string, int>();
    foreach (var c in counts) {
      asDict[c.Word] = c.Count;
    }
    Assert.Equal(3, asDict["a"]);
    Assert.Equal(2, asDict["b"]);
    Assert.Equal(1, asDict["c"]);
  }

  [Fact]
  public void CountWords_Empty_ReturnsEmptyList() {
    Assert.Empty(WordAnalyzer.CountWords(new List<string>()));
  }

  [Fact]
  public void TopNWords_Basic_ReturnsDescendingByCount() {
    var counts = new List<WordCount> {
      new WordCount("a", 3), new WordCount("b", 5), new WordCount("c", 1),
    };
    var top = WordAnalyzer.TopNWords(counts, 2);
    Assert.Equal(2, top.Count);
    Assert.Equal("b", top[0].Word);
    Assert.Equal("a", top[1].Word);
  }

  [Fact]
  public void TopNWords_NLargerThanList_ReturnsAll() {
    var counts = new List<WordCount> { new WordCount("a", 1) };
    var top = WordAnalyzer.TopNWords(counts, 5);
    Assert.Single(top);
  }

  [Fact]
  public void TopNWords_NZero_ReturnsEmpty() {
    var counts = new List<WordCount> { new WordCount("a", 1) };
    Assert.Empty(WordAnalyzer.TopNWords(counts, 0));
  }

  [Fact]
  public void TopNWords_DoesNotMutateInput() {
    var counts = new List<WordCount> {
      new WordCount("a", 1), new WordCount("b", 2),
    };
    WordAnalyzer.TopNWords(counts, 1);
    Assert.Equal(2, counts.Count);
    Assert.Equal("a", counts[0].Word);
  }

  [Fact]
  public void Analyze_EndToEnd_ReturnsExpectedTopWords() {
    var text = "the cat sat on the mat the cat ran";
    var result = WordAnalyzer.Analyze(text, 2);
    Assert.Equal(2, result.Count);
    Assert.Equal("the", result[0].Word);
    Assert.Equal(3, result[0].Count);
    Assert.Equal("cat", result[1].Word);
    Assert.Equal(2, result[1].Count);
  }
}

public class ProgramTests {
  [Fact]
  public void ParseArgs_Defaults_UsesTopTen() {
    var parsed = Program.ParseArgs(new[] { "file.txt" });
    Assert.Equal("file.txt", parsed.FilePath);
    Assert.Equal(10, parsed.TopN);
  }

  [Fact]
  public void ParseArgs_CustomTop_UsesGivenValue() {
    var parsed = Program.ParseArgs(new[] { "file.txt", "-n", "3" });
    Assert.Equal(3, parsed.TopN);
  }

  [Fact]
  public void ParseArgs_LongOption_UsesGivenValue() {
    var parsed = Program.ParseArgs(new[] { "file.txt", "--top", "7" });
    Assert.Equal(7, parsed.TopN);
  }

  [Fact]
  public void ParseArgs_MissingValueForTop_Throws() {
    Assert.Throws<ArgumentException>(
        () => Program.ParseArgs(new[] { "file.txt", "-n" }));
  }

  [Fact]
  public void ParseArgs_InvalidIntegerForTop_Throws() {
    Assert.Throws<ArgumentException>(
        () => Program.ParseArgs(new[] { "file.txt", "-n", "abc" }));
  }

  [Fact]
  public void ParseArgs_UnexpectedExtraArgument_Throws() {
    Assert.Throws<ArgumentException>(
        () => Program.ParseArgs(new[] { "file.txt", "extra" }));
  }

  [Fact]
  public void ParseArgs_MissingFile_Throws() {
    Assert.Throws<ArgumentException>(
        () => Program.ParseArgs(Array.Empty<string>()));
  }

  [Fact]
  public void Main_Success_PrintsTopWords() {
    var path = Path.GetTempFileName();
    try {
      File.WriteAllText(path, "dog dog cat");
      var originalOut = Console.Out;
      using var writer = new StringWriter();
      Console.SetOut(writer);
      int exitCode;
      try {
        exitCode = Program.Main(new[] { path, "-n", "2" });
      } finally {
        Console.SetOut(originalOut);
      }
      Assert.Equal(0, exitCode);
      Assert.Contains("dog\t2", writer.ToString());
      Assert.Contains("cat\t1", writer.ToString());
    } finally {
      File.Delete(path);
    }
  }

  [Fact]
  public void Main_FileNotFound_ReturnsOneAndPrintsError() {
    var path = Path.Combine(Path.GetTempPath(), "does_not_exist_wordstat.txt");
    if (File.Exists(path)) {
      File.Delete(path);
    }
    var originalErr = Console.Error;
    using var writer = new StringWriter();
    Console.SetError(writer);
    int exitCode;
    try {
      exitCode = Program.Main(new[] { path });
    } finally {
      Console.SetError(originalErr);
    }
    Assert.Equal(1, exitCode);
    Assert.Contains("error: file not found", writer.ToString());
  }

  [Fact]
  public void Main_BadArguments_ReturnsOneAndPrintsError() {
    var originalErr = Console.Error;
    using var writer = new StringWriter();
    Console.SetError(writer);
    int exitCode;
    try {
      exitCode = Program.Main(Array.Empty<string>());
    } finally {
      Console.SetError(originalErr);
    }
    Assert.Equal(1, exitCode);
    Assert.Contains("error:", writer.ToString());
  }
}
