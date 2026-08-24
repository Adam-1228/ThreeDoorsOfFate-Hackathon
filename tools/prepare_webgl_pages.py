from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


SHELL_MARKER = 'data-tdf-pages-shell="v1"'

PAGE_STYLE = """  <style data-tdf-style="v1">
    :root { color-scheme: dark; }
    body.tdf-page {
      box-sizing: border-box;
      min-height: 100vh;
      margin: 0;
      padding: 0 16px 48px;
      overflow-x: hidden;
      display: flex;
      flex-direction: column;
      align-items: center;
      background:
        radial-gradient(circle at 50% -10%, rgba(139, 92, 246, 0.20), transparent 38rem),
        #09070f;
      color: #f5f1ff;
      font-family: Inter, Pretendard, "Noto Sans KR", system-ui, sans-serif;
    }
    body.tdf-page *, body.tdf-page *::before, body.tdf-page *::after {
      box-sizing: inherit;
    }
    .tdf-hero, #how-to-play {
      width: min(960px, 100%);
    }
    .tdf-hero {
      margin: 28px auto 16px;
      text-align: center;
    }
    .tdf-hero h1 {
      margin: 0 0 8px;
      color: #f4df9b;
      font-family: Georgia, "Noto Serif KR", serif;
      font-size: clamp(1.65rem, 4vw, 2.6rem);
      letter-spacing: 0.04em;
    }
    .tdf-loading-notice {
      margin: 0;
      padding: 11px 16px;
      border: 1px solid rgba(244, 223, 155, 0.42);
      border-radius: 12px;
      background: rgba(24, 18, 38, 0.88);
      color: #eee7ff;
      line-height: 1.55;
    }
    body.tdf-page #unity-container.unity-desktop,
    body.tdf-page #unity-container.unity-mobile {
      position: relative;
      inset: auto;
      left: auto;
      top: auto;
      width: min(960px, 100%);
      height: auto;
      margin: 0 auto;
      transform: none;
    }
    body.tdf-page #unity-canvas,
    body.tdf-page #unity-canvas.unity-mobile {
      display: block;
      width: 100% !important;
      height: auto !important;
      aspect-ratio: 8 / 5;
      border-radius: 8px 8px 0 0;
      outline: 1px solid rgba(244, 223, 155, 0.25);
    }
    body.tdf-page #unity-footer {
      position: relative;
      min-height: 38px;
    }
    #how-to-play {
      margin: 26px auto 0;
      padding: clamp(18px, 3vw, 30px);
      border: 1px solid rgba(167, 139, 250, 0.38);
      border-radius: 16px;
      background: rgba(18, 13, 30, 0.94);
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.28);
    }
    #how-to-play h2 {
      margin: 0 0 18px;
      color: #f4df9b;
      font-size: clamp(1.35rem, 3vw, 1.85rem);
    }
    .tdf-guide-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 12px;
      margin: 0;
      padding: 0;
      list-style: none;
    }
    .tdf-guide-grid li {
      min-height: 94px;
      padding: 15px;
      border-radius: 12px;
      background: rgba(255, 255, 255, 0.055);
      line-height: 1.55;
    }
    .tdf-step {
      display: inline-grid;
      width: 1.8rem;
      height: 1.8rem;
      margin-right: 8px;
      place-items: center;
      border-radius: 50%;
      background: #7357bd;
      color: #fff;
      font-weight: 700;
    }
    .tdf-guide-grid strong { color: #fff4c7; }
    .tdf-guide-grid p { margin: 7px 0 0; color: #d8d0e8; }
    .tdf-tip {
      margin: 16px 0 0;
      color: #cfc5e5;
      line-height: 1.55;
    }
    @media (max-width: 760px) {
      body.tdf-page { padding-inline: 8px; }
      .tdf-hero { margin-top: 16px; }
      .tdf-guide-grid { grid-template-columns: 1fr; }
      #how-to-play { border-radius: 12px; }
    }
  </style>
"""

LOADING_NOTICE = """  <header class="tdf-hero" data-tdf-pages-shell="v1">
    <h1>Three Doors of Fate</h1>
    <p class="tdf-loading-notice" role="status">
      첫 실행은 대용량 게임 데이터를 내려받습니다. 로딩 창을 닫지 말고 진행률이 100%가 될 때까지 기다려 주세요.
    </p>
  </header>
"""

PLAY_GUIDE = """  <section id="how-to-play" aria-labelledby="how-to-play-title">
    <h2 id="how-to-play-title">플레이 방법 <small lang="en">/ How to Play</small></h2>
    <ol class="tdf-guide-grid">
      <li><span class="tdf-step">1</span><strong>게임 시작 → 난이도 → 직업 선택</strong><p>도박사·점술가·추방자 중 한 명을 선택합니다.</p></li>
      <li><span class="tdf-step">2</span><strong>세 개의 문 중 하나를 선택</strong><p>단서와 현재 자원을 비교해 다음 방의 위험을 결정합니다.</p></li>
      <li><span class="tdf-step">3</span><strong>카드를 클릭해 사용하고 턴 종료</strong><p>행동력, 적의 의도, 방어와 상태 효과를 확인하세요.</p></li>
      <li><span class="tdf-step">4</span><strong>체력·금화·빚·덱을 관리</strong><p>보상과 상점, 이벤트를 활용해 10개 방과 보스를 돌파합니다.</p></li>
    </ol>
    <p class="tdf-tip">게임 안의 <strong>플레이 방법</strong> 버튼에서 5단계 그림 안내를 다시 볼 수 있습니다. 우측 하단 버튼으로 전체 화면을 사용할 수 있습니다.</p>
  </section>
"""


def _add_body_class(html: str) -> str:
    match = re.search(r"<body(?P<attrs>[^>]*)>", html, flags=re.IGNORECASE)
    if match is None:
        raise ValueError("index.html has no body element")

    attrs = match.group("attrs")
    class_match = re.search(
        r'\bclass\s*=\s*"(?P<classes>[^"]*)"',
        attrs,
        flags=re.IGNORECASE,
    )
    if class_match is None:
        replacement = f'<body{attrs} class="tdf-page">'
    else:
        classes = class_match.group("classes").split()
        if "tdf-page" not in classes:
            classes.append("tdf-page")
        updated_attrs = (
            attrs[: class_match.start("classes")]
            + " ".join(classes)
            + attrs[class_match.end("classes") :]
        )
        replacement = f"<body{updated_attrs}>"

    return html[: match.start()] + replacement + html[match.end() :]


def prepare_site(site_root: Path, expected_version: str | None = None) -> bool:
    index_path = site_root / "index.html"
    if not index_path.is_file():
        raise ValueError(f"WebGL index was not found: {index_path}")

    html = index_path.read_text(encoding="utf-8")
    if expected_version is not None:
        version_match = re.search(
            r'\bproductVersion\s*:\s*"(?P<version>[^"]+)"',
            html,
        )
        actual_version = (
            version_match.group("version") if version_match is not None else None
        )
        if actual_version != expected_version:
            raise ValueError(
                "expected product version "
                f"{expected_version}, found {actual_version or 'none'}"
            )

    if SHELL_MARKER in html:
        return False
    if 'id="unity-container"' not in html:
        raise ValueError("index.html has no unity-container")
    if "</head>" not in html or "</body>" not in html:
        raise ValueError("index.html is missing a closing head or body element")

    updated = _add_body_class(html)
    updated = updated.replace("</head>", PAGE_STYLE + "</head>", 1)
    updated = updated.replace(
        '<div id="unity-container"',
        LOADING_NOTICE + '<div id="unity-container"',
        1,
    )
    updated = updated.replace("</body>", PLAY_GUIDE + "</body>", 1)

    temporary_path = index_path.with_suffix(index_path.suffix + ".tmp")
    try:
        temporary_path.write_text(updated, encoding="utf-8", newline="\n")
        temporary_path.replace(index_path)
    finally:
        temporary_path.unlink(missing_ok=True)
    return True


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Add the Three Doors of Fate play guide to a Unity WebGL site."
    )
    parser.add_argument(
        "--site-root",
        required=True,
        type=Path,
        help="Directory containing the generated Unity index.html.",
    )
    parser.add_argument(
        "--expected-version",
        help="Reject an artifact whose Unity productVersion does not match.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    try:
        changed = prepare_site(
            args.site_root.resolve(),
            expected_version=args.expected_version,
        )
    except (OSError, ValueError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 1

    state = "updated" if changed else "already prepared"
    print(f"WebGL Pages shell {state}: {args.site_root.resolve() / 'index.html'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
