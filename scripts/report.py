#!/usr/bin/env python3
"""Build report.md from spec.json and check.json.

Usage:
  report.py SPEC.json CHECK.json --out report.md [--sheet sheet.png] [--visual "1:pass,2:fail:holes under arm,..."] [--credits N] [--notes "..."]

The report is the durable record of a version: what was asked, what was measured,
what failed, and which stage a fix belongs to. Keep chat replies short and point here.
"""
import argparse
import json
import os


def fmt(v, nd=2):
    if isinstance(v, float):
        return f"{v:.{nd}f}"
    return "-" if v in (None, "") else str(v)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("spec")
    ap.add_argument("check")
    ap.add_argument("--out", required=True)
    ap.add_argument("--sheet", default="")
    ap.add_argument("--visual", default="")
    ap.add_argument("--credits", default="")
    ap.add_argument("--notes", default="")
    a = ap.parse_args()

    spec = json.load(open(a.spec))
    chk = json.load(open(a.check))

    rows = [
        ("Height (m)", fmt(spec.get("height_m")), fmt(chk.get("height_m"))),
        ("Pivot", spec.get("pivot", "-"), chk.get("pivotVerdict", "-")),
        ("Triangles", fmt(spec.get("max_triangles")), fmt(chk.get("triangles"))),
        ("Materials", fmt(spec.get("max_materials")), fmt(chk.get("materialCount"))),
        ("Texture max (px)", fmt(spec.get("max_texture")), fmt(max([m.get("mainTextureSize", 0) for m in chk.get("materials", [])] or [0]))),
        ("Rig", spec.get("rig", "none"), "humanoid" if chk.get("avatarHuman") else ("animator" if chk.get("hasAnimator") else "none")),
        ("Clips", ", ".join(x.get("name", "") for x in spec.get("animations", [])) or "-", ", ".join(c.get("name", "") for c in chk.get("clips", [])) or "-"),
        ("Source model", "-", chk.get("sourceModel", "-")),
        ("Import scale", "-", fmt(chk.get("importScale"), 3)),
    ]

    failures = chk.get("failures", [])
    warnings = chk.get("warnings", [])
    visual_fails = [v for v in a.visual.split(",") if v and ":fail" in v]

    lines = []
    lines.append(f"# {spec.get('name', spec.get('slug', 'asset'))}  ({spec.get('slug', '')})")
    lines.append("")
    status = "PASS" if not failures and not visual_fails else "FAIL"
    lines.append(f"**Result:** {status}  ·  **Prefab:** `{chk.get('prefab', '-')}`  ·  **Style:** {spec.get('style', '-')}  ·  **Platform:** {spec.get('platform', '-')}")
    if a.credits:
        lines.append(f"**Credits used:** {a.credits}")
    lines.append("")
    if a.sheet:
        lines.append(f"![review sheet]({os.path.basename(a.sheet)})")
        lines.append("")
    lines.append("| Property | Spec | Actual |")
    lines.append("|---|---|---|")
    for name, s, v in rows:
        lines.append(f"| {name} | {s} | {v} |")
    lines.append("")
    lines.append("## Fix list")
    if failures:
        for f in failures:
            lines.append(f"- {f}")
    else:
        lines.append("- none from the automated checks")
    for v in visual_fails:
        parts = v.split(":", 2)
        lines.append(f"- visual rubric {parts[0]}: {parts[2] if len(parts) > 2 else 'fail'}")
    if warnings:
        lines.append("")
        lines.append("## Warnings")
        for w in warnings:
            lines.append(f"- {w}")
    if a.notes:
        lines.append("")
        lines.append("## Notes")
        lines.append(a.notes)
    lines.append("")
    lines.append("## Art direction")
    lines.append(spec.get("artDirection", "") or "-")
    lines.append("")

    with open(a.out, "w") as fh:
        fh.write("\n".join(lines))
    print(a.out, status, f"{len(failures)} failures, {len(warnings)} warnings")


if __name__ == "__main__":
    main()
