#!/usr/bin/env python3
import json
import sys
import subprocess

def main():
    try:
        # Git の変更ファイル一覧を取得
        res_status = subprocess.run(
            ["git", "status", "--short"],
            capture_output=True,
            text=True,
            timeout=3
        )
        status_out = res_status.stdout.strip()

        if not status_out:
            print(json.dumps({"injectSteps": []}))
            return

        # 変更のサマリーを取得
        res_diff = subprocess.run(
            ["git", "diff", "--stat"],
            capture_output=True,
            text=True,
            timeout=3
        )
        diff_out = res_diff.stdout.strip()

        message = (
            "【ファイル変更サマリー】\n"
            f"▼ 変更・新規作成ファイル一覧:\n{status_out}\n"
        )
        if diff_out:
            message += f"\n▼ 差分統計:\n{diff_out}\n"

        message += "\n※タスク完了時には、上記変更ファイル一覧と具体的な変更内容をチャットの返答に必ず記載してください。"

        result = {
            "injectSteps": [
                {
                    "ephemeralMessage": message
                }
            ]
        }
        print(json.dumps(result))
    except Exception:
        print(json.dumps({"injectSteps": []}))

if __name__ == "__main__":
    main()

