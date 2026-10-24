"""Объединяет строки Cobertura из всех тестовых проектов без усреднения процентов."""
import argparse
from collections import defaultdict
from pathlib import Path
import xml.etree.ElementTree as ET
import sys


def collect(directory, root):
    files = defaultdict(dict)
    reports = list(directory.rglob("coverage.cobertura.xml"))
    if not reports:
        raise SystemExit(f"Нет отчётов Cobertura: {directory}")
    for report in reports:
        tree = ET.parse(report)
        sources = [Path(node.text) for node in tree.findall("./sources/source")]
        for cls in tree.iter("class"):
            filename = cls.attrib["filename"]
            candidates = [Path(filename)] if Path(filename).is_absolute() else [source / filename for source in sources]
            path = next((candidate.resolve() for candidate in candidates if candidate.resolve().is_relative_to(root)), None)
            if path is None:
                raise SystemExit(f"Исходник вне репозитория в {report}: {filename}")
            name = path.relative_to(root).as_posix()
            if not name.endswith(".cs") or any(part in name.split("/") for part in ("obj", "Migrations")):
                continue
            for line in cls.findall("./lines/line"):
                number, hits = int(line.attrib["number"]), int(line.attrib["hits"])
                files[name][number] = max(files[name].get(number, 0), hits)
    return files


def score(files, prefix=""):
    lines = [hits for name, contents in files.items() if name.startswith(prefix) for hits in contents.values()]
    covered = sum(hits > 0 for hits in lines)
    return f"{covered}/{len(lines)} ({100 * covered / len(lines):.1f}%)" if lines else "—"


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("current", type=Path)
    parser.add_argument("--baseline", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    current = collect(args.current.resolve(), root)
    baseline = collect(args.baseline.resolve(), root) if args.baseline else {}
    rows = ["# Покрытие исполняемых строк", "", "Миграции, сгенерированный код и XAML исключены из числовой метрики. Code-behind включён.",
            "Строка считается покрытой, если выполнена хотя бы одним тестовым проектом; проценты проектов не усредняются.",
            "", "| Область | До | После |", "|---|---:|---:|"]
    for label, prefix in [("Весь runtime C#", ""), ("Bonfire", "Bonfire/"), ("Модели и маппинг", "Bonfire/Models/"),
                          ("Сервисы", "Bonfire/Services/"), ("ViewModels", "Bonfire/ViewModels/"),
                          ("Инфраструктура", "Bonfire/Infrastructure/"), ("WPF code-behind", "Bonfire/Views/"),
                          ("BonfireDB", "BonfireDB/"), ("MoonCalendar", "MoonCalendar/")]:
        rows.append(f"| {label} | {score(baseline, prefix)} | {score(current, prefix)} |")
    rows += ["", "## По файлам", "", "| Файл | До | После |", "|---|---:|---:|"]
    for name in sorted(current):
        rows.append(f"| `{name}` | {score(baseline, name)} | {score(current, name)} |")
    result = "\n".join(rows) + "\n"
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(result, encoding="utf-8")
    print("\n".join(rows[:16]))


if __name__ == "__main__":
    main()
