import json
import re
import os
import sys

def clean_user_content(text):
    if not text:
        return ""
    text = re.sub(r'<USER_REQUEST>\s*', '', text)
    text = re.sub(r'\s*</USER_REQUEST>', '', text)
    text = re.sub(r'<ADDITIONAL_METADATA>[\s\S]*?</ADDITIONAL_METADATA>', '', text)
    return text.strip()

def export_conversation():
    transcript_path = r"C:\Users\ADMIN\.gemini\antigravity\brain\de6915e6-52f0-46be-9959-36fe98a11139\.system_generated\logs\transcript_full.jsonl"
    if not os.path.exists(transcript_path):
        transcript_path = r"C:\Users\ADMIN\.gemini\antigravity\brain\de6915e6-52f0-46be-9959-36fe98a11139\.system_generated\logs\transcript.jsonl"

    messages = []
    
    with open(transcript_path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                data = json.loads(line)
            except Exception:
                continue
            
            source = data.get("source")
            msg_type = data.get("type")
            content = data.get("content")
            
            if source == "USER_EXPLICIT" and msg_type == "USER_INPUT":
                clean_c = clean_user_content(content)
                if clean_c:
                    messages.append({
                        "role": "User",
                        "content": clean_c,
                        "time": data.get("created_at", "")
                    })
            elif source == "MODEL" and msg_type == "PLANNER_RESPONSE" and content:
                # Filter out pure system messages or non-user facing
                if isinstance(content, str) and content.strip():
                    messages.append({
                        "role": "Antigravity (AI)",
                        "content": content.strip(),
                        "time": data.get("created_at", "")
                    })

    # Deduplicate consecutive identical messages if any
    filtered = []
    for m in messages:
        if filtered and filtered[-1]["role"] == m["role"] and filtered[-1]["content"] == m["content"]:
            continue
        filtered.append(m)

    os.makedirs("docs", exist_ok=True)
    md_path = "docs/CHAT_TRANSCRIPT.md"
    html_path = "docs/CHAT_TRANSCRIPT.html"

    # Write Markdown
    with open(md_path, "w", encoding="utf-8") as f:
        f.write("# 💬 Antigravity Pair-Programming Session Transcript\n\n")
        f.write("> **Project:** Caro Arena (Infinite Gomoku Online .NET 9)\n")
        f.write("> **Session ID:** `de6915e6-52f0-46be-9959-36fe98a11139`\n")
        f.write("> **Repository:** [https://github.com/NuclearLoli/TIC-TAC-TOE](https://github.com/NuclearLoli/TIC-TAC-TOE)\n\n---\n\n")
        
        for msg in filtered:
            role = msg["role"]
            time_str = msg["time"][:19].replace("T", " ") if msg["time"] else ""
            if role == "User":
                f.write(f"### 👤 Người Dùng ({time_str})\n\n")
                f.write(f"{msg['content']}\n\n---\n\n")
            else:
                f.write(f"### 🤖 Antigravity Assistant ({time_str})\n\n")
                f.write(f"{msg['content']}\n\n---\n\n")

    # Write HTML for instant visual sharing in any browser
    html_template = f"""<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Antigravity Chat Session - Caro Game .NET 9</title>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.5.0/github-markdown-dark.min.css">
    <script src="https://cdn.jsdelivr.net/npm/marked/marked.min.js"></script>
    <style>
        body {{
            background-color: #0d1117;
            color: #c9d1d9;
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif;
            margin: 0;
            padding: 20px;
        }}
        .container {{
            max-width: 900px;
            margin: 0 auto;
        }}
        .header {{
            text-align: center;
            padding: 20px 0;
            border-bottom: 1px solid #30363d;
            margin-bottom: 30px;
        }}
        .header h1 {{
            color: #58a6ff;
            margin-bottom: 8px;
        }}
        .msg {{
            margin-bottom: 25px;
            display: flex;
            flex-direction: column;
        }}
        .msg-header {{
            font-size: 0.85em;
            margin-bottom: 6px;
            display: flex;
            align-items: center;
            gap: 8px;
        }}
        .user .msg-header {{
            color: #7ee787;
        }}
        .assistant .msg-header {{
            color: #79c0ff;
        }}
        .bubble {{
            padding: 16px 20px;
            border-radius: 12px;
            line-height: 1.6;
        }}
        .user .bubble {{
            background: #161b22;
            border: 1px solid #238636;
            border-left: 4px solid #2ea043;
        }}
        .assistant .bubble {{
            background: #161b22;
            border: 1px solid #30363d;
            border-left: 4px solid #1f6feb;
        }}
        pre {{
            background: #0d1117 !important;
            border-radius: 6px;
            padding: 12px;
            overflow-x: auto;
        }}
        code {{
            color: #ff7b72;
            font-family: Consolas, monospace;
        }}
        a {{
            color: #58a6ff;
        }}
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🎮 Caro Arena: Phiên Phát Triển & Triển Khai .NET 9</h1>
            <p>Nhật ký toàn bộ cuộc trò chuyện lập trình viên cùng Antigravity AI Assistant</p>
            <p><a href="https://github.com/NuclearLoli/TIC-TAC-TOE" target="_blank">🔗 Repository: NuclearLoli/TIC-TAC-TOE</a></p>
        </div>
        <div id="chat"></div>
    </div>
    <script>
        const rawMessages = {json.dumps(filtered, ensure_ascii=False)};
        const chatDiv = document.getElementById('chat');
        
        rawMessages.forEach(m => {{
            const div = document.createElement('div');
            const isUser = m.role === 'User';
            div.className = 'msg ' + (isUser ? 'user' : 'assistant');
            
            const timeTag = m.time ? m.time.substring(0, 19).replace('T', ' ') : '';
            div.innerHTML = `
                <div class="msg-header">
                    <strong>${{isUser ? '👤 Người Dùng' : '🤖 Antigravity Assistant'}}</strong>
                    <span style="color:#8b949e">${{timeTag}}</span>
                </div>
                <div class="bubble markdown-body">${{marked.parse(m.content)}}</div>
            `;
            chatDiv.appendChild(div);
        }});
    </script>
</body>
</html>
"""
    with open(html_path, "w", encoding="utf-8") as f:
        f.write(html_template)

    print(f"Exported {len(filtered)} messages successfully:")
    print(f" -> {os.path.abspath(md_path)}")
    print(f" -> {os.path.abspath(html_path)}")

if __name__ == "__main__":
    export_conversation()
