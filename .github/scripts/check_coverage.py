#!/usr/bin/env python3
"""Fails (exit 1) when the line coverage of one namespace in a Cobertura file is below a threshold.

Usage: check_coverage.py <coverage.cobertura.xml> <class-name-prefix> <minimum-percent>
Example: check_coverage.py coverage.cobertura.xml Mobile_apps_L1_Model_view.ViewModels. 60

A line counts once even when several classes (compiler-generated closures, async state machines of one ViewModel)
report it, so the percentage is the share of distinct (file, line) pairs that were hit at least once.
"""
import sys
import xml.etree.ElementTree as ET


def namespace_coverage(path: str, prefix: str) -> tuple[int, int]:
    lines: dict[tuple[str, int], bool] = {}
    for cls in ET.parse(path).getroot().iter("class"):
        if not cls.get("name", "").startswith(prefix):
            continue
        filename = cls.get("filename", "")
        for line in cls.iter("line"):
            key = (filename, int(line.get("number")))
            lines[key] = lines.get(key, False) or int(line.get("hits")) > 0
    return sum(lines.values()), len(lines)


def main(argv: list[str]) -> int:
    if len(argv) != 4:
        print(__doc__)
        return 2
    path, prefix, minimum = argv[1], argv[2], float(argv[3])
    covered, total = namespace_coverage(path, prefix)
    if total == 0:
        print(f"No lines found for '{prefix}' in {path}")
        return 2
    percent = 100.0 * covered / total
    print(f"Line coverage of '{prefix}*': {percent:.1f}% ({covered}/{total} lines), minimum {minimum:.0f}%")
    if percent < minimum:
        print("Coverage gate FAILED")
        return 1
    print("Coverage gate passed")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
