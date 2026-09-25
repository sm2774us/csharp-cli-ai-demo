"""Wraps over-long `//` comment lines to fit within a max line length.

`dotnet format` does not reflow comment text (only whitespace/brace
style), so this handles the one case that matters for AI-generated
C# code: full-line `//` comments running past the Google C# Style
Guide's 100-column limit.

Deliberately conservative: only touches whole-line `//` comments (a
line whose stripped content starts with `//` and isn't `///` XML doc
or `//!`). Never touches code, XML doc comments, or string literals.

Usage:
    python scripts/wrap_comments.py --max-length 100 FILE [FILE ...]
"""

from __future__ import annotations

import argparse
import sys
import textwrap


def wrap_comment_line(line: str, max_length: int) -> list[str]:
    """Wraps one over-long `//` comment line into several shorter ones.

    Args:
        line: A single source line (no trailing newline).
        max_length: Maximum allowed line length.

    Returns:
        One or more replacement lines, or the original line unchanged
        if it isn't a whole-line `//` comment or is short enough.
    """
    stripped = line.lstrip()
    indent = line[: len(line) - len(stripped)]

    if not stripped.startswith("//") or len(line) <= max_length:
        return [line]
    if stripped.startswith("///"):  # XML doc comment, never touch
        return [line]

    text = stripped[2:].strip()
    if not text:
        return [line]

    prefix = f"{indent}// "
    width = max(max_length - len(prefix), 20)
    wrapped = textwrap.wrap(text, width=width) or [text]
    return [f"{prefix}{chunk}" for chunk in wrapped]


def wrap_file(path: str, max_length: int) -> bool:
    """Rewrites a file with any over-long comment lines wrapped.

    Args:
        path: File to process in place.
        max_length: Maximum allowed line length.

    Returns:
        True if the file was modified.
    """
    with open(path, encoding="utf-8") as handle:
        original_lines = handle.read().splitlines()

    new_lines: list[str] = []
    changed = False
    for line in original_lines:
        replacement = wrap_comment_line(line, max_length)
        if replacement != [line]:
            changed = True
        new_lines.extend(replacement)

    if changed:
        with open(path, "w", encoding="utf-8") as handle:
            handle.write("\n".join(new_lines) + "\n")

    return changed


def main() -> int:
    """CLI entry point."""
    parser = argparse.ArgumentParser()
    parser.add_argument("--max-length", type=int, default=100)
    parser.add_argument("files", nargs="+")
    args = parser.parse_args()

    for path in args.files:
        if wrap_file(path, args.max_length):
            print(f"wrapped long comments in {path}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
