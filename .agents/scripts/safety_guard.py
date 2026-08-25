#!/usr/bin/env python3
import json
import sys
import re

DANGEROUS_PATTERNS = [
    (r"\brm\s+-[a-zA-Z]*r[a-zA-Z]*f\b", "強制再帰削除 (rm -rf) が検出されました"),
    (r"\brm\s+-[a-zA-Z]*f[a-zA-Z]*r\b", "強制再帰削除 (rm -fr) が検出されました"),
    (r"\bgit\s+reset\s+--hard\b", "Gitのハードリセット (git reset --hard) が検出されました"),
    (r"\bgit\s+clean\s+-[a-zA-Z]*f\b", "Gitの未追跡ファイル強制削除 (git clean -f) が検出されました"),
    (r"\bgit\s+checkout\s+--\s+\.", "全変更の破棄 (git checkout -- .) が検出されました"),
    (r"\bgit\s+restore\s+\.", "全変更の破棄 (git restore .) が検出されました"),
    (r"\b(drop|truncate)\s+table\b", "データベーステーブル削除/切り捨て操作が検出されました"),
    (r"\b(format|diskutil|dd)\b", "ディスク操作/初期化コマンドが検出されました"),
    (r"\bchmod\s+-R\s+777\b", "危険なパーミッション変更 (chmod -R 777) が検出されました"),
    (r"\bkill\s+-9\b", "プロセス強制終了 (kill -9) が検出されました"),
]

def main():
    try:
        raw_input = sys.stdin.read()
        if not raw_input.strip():
            print(json.dumps({"decision": "allow"}))
            return

        data = json.loads(raw_input)
        tool_call = data.get("toolCall", {})
        tool_name = tool_call.get("name", "")

        if tool_name == "run_command":
            args = tool_call.get("args", {})
            command_line = args.get("CommandLine", "")
            for pattern, reason in DANGEROUS_PATTERNS:
                if re.search(pattern, command_line, re.IGNORECASE):
                    result = {
                        "decision": "force_ask",
                        "reason": f"【安全ガード】{reason}: `{command_line}`"
                    }
                    print(json.dumps(result))
                    return

        print(json.dumps({"decision": "allow"}))
    except Exception as e:
        # エラー時は安全のためにユーザーに確認を求める
        print(json.dumps({
            "decision": "ask",
            "reason": f"安全チェック処理中にエラーが発生しました: {str(e)}"
        }))

if __name__ == "__main__":
    main()

