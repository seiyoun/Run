#!/usr/bin/env python3
import json
import sys

def main():
    message = (
        "【YAGNI原則・不要コード生成の厳禁】\n"
        "・ユーザーから明示指示のないクラス、ステート、Enum値、フィールド、プロパティ、メソッドは一切生成・追加しないでください。\n"
        "・「将来使うかもしれない」「念のため」「あると便利そう」という推測に基づく先行実装（デッドコード）の作成は厳禁です。\n"
        "・作成・追加するコードは必ず『今この瞬間に呼び出し元（参照元）が存在する』最小限の実装（Minimal Viable Change）に留めてください。"
    )
    result = {
        "injectSteps": [
            {
                "ephemeralMessage": message
            }
        ]
    }
    print(json.dumps(result, ensure_ascii=False))

if __name__ == "__main__":
    main()

