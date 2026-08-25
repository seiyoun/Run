#!/usr/bin/env python3
import json
import sys
import os
import re
import urllib.request
import urllib.error
import subprocess

def check_unity_mcp_bridge():
    """Unity MCP Bridge (http://127.0.0.1:8088/logs) からログを取得してエラーを検証"""
    try:
        req = urllib.request.Request("http://127.0.0.1:8088/logs", headers={"User-Agent": "AntigravityHook"})
        with urllib.request.urlopen(req, timeout=1.5) as res:
            if res.status == 200:
                data = json.loads(res.read().decode("utf-8"))
                logs = data if isinstance(data, list) else data.get("logs", [])
                errors = []
                for entry in logs:
                    log_type = entry.get("type", "") or entry.get("logType", "")
                    message = entry.get("message", "")
                    if log_type in ("Error", "Exception", "Assert") or "error CS" in message:
                        errors.append(f"[{log_type}] {message}")
                if errors:
                    return "\n".join(errors[:5])
    except Exception:
        pass
    return None

def check_unity_editor_log():
    """~/Library/Logs/Unity/Editor.log から最新のコンパイルエラーを検出"""
    log_path = os.path.expanduser("~/Library/Logs/Unity/Editor.log")
    if not os.path.exists(log_path):
        return None

    try:
        file_size = os.path.getsize(log_path)
        read_size = min(file_size, 50 * 1024)
        with open(log_path, "r", encoding="utf-8", errors="ignore") as f:
            if file_size > read_size:
                f.seek(file_size - read_size)
            lines = f.readlines()

        error_lines = []
        compile_error_pattern = re.compile(r"(?:[A-Za-z0-9_\-/\\]+\.cs\(\d+,\d+\):\s*error\s*CS\d+:|Compilation failed:)", re.IGNORECASE)

        for line in lines[-200:]:
            if compile_error_pattern.search(line):
                error_lines.append(line.strip())

        if error_lines:
            return "\n".join(error_lines[:5])
    except Exception:
        pass
    return None

def check_git_conflicts():
    """Git のコンフリクトマーカーを検出"""
    try:
        res = subprocess.run(
            ["git", "diff", "--check"],
            capture_output=True,
            text=True,
            timeout=5
        )
        if res.returncode != 0 and "conflict" in res.stdout.lower():
            return "未解決のGitコンフリクトマーカーが残っています。"
    except Exception:
        pass
    return None

def main():
    try:
        raw_input = sys.stdin.read()
        
        # 1. Git コンフリクトチェック
        git_err = check_git_conflicts()
        if git_err:
            sys.stderr.write(f"【バリデーションエラー】{git_err}\n")
            print(json.dumps({
                "decision": "continue",
                "reason": f"【バリデーションエラー】{git_err}"
            }))
            return

        # 2. Unity MCP Bridge チェック
        mcp_err = check_unity_mcp_bridge()
        if mcp_err:
            sys.stderr.write(f"【Unityコンパイルエラー】\n{mcp_err}\n")
            print(json.dumps({
                "decision": "continue",
                "reason": f"【Unityコンパイルエラー (MCP)】以下のエラーを修正してください:\n{mcp_err}"
            }))
            return

        # 3. Unity Editor.log チェック
        log_err = check_unity_editor_log()
        if log_err:
            sys.stderr.write(f"【Unityコンパイルエラー】\n{log_err}\n")
            print(json.dumps({
                "decision": "continue",
                "reason": f"【Unityコンパイルエラー (Editor.log)】以下のエラーを修正してください:\n{log_err}"
            }))
            return

        # エラーがなければ正常終了
        print(json.dumps({}))
    except Exception:
        print(json.dumps({}))

if __name__ == "__main__":
    main()
