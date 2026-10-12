import os
import re
import sys
import config

DEFAULT_LANG = "en-US"
SUPPORTED_LANGS = ("ja-JP", "en-US")
# メニューに表示する言語名（各言語の表記のまま。翻訳しない）
LANGUAGE_NAMES = {"日本語": "ja-JP", "English": "en-US"}

def _base_dir():
    if getattr(sys, 'frozen', False):
        return sys._MEIPASS
    return os.path.dirname(os.path.abspath(__file__))

def _detect_lang():
    """Windowsの表示言語が日本語なら ja-JP、それ以外は en-US を使う"""
    try:
        import ctypes
        if (ctypes.windll.kernel32.GetUserDefaultUILanguage() & 0x3FF) == 0x11:
            return "ja-JP"
    except Exception:
        pass
    return DEFAULT_LANG

def _load_table(lang):
    """languages/<lang>.lang を読み込む（形式: key=値）"""
    table = {}
    path = os.path.join(_base_dir(), "languages", f"{lang}.lang")
    if os.path.exists(path):
        with open(path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line or line.startswith("#") or "=" not in line:
                    continue
                key, value = line.split("=", 1)
                table[key.strip()] = value.strip().replace("\\n", "\n")
    return table

CONFIG_PATH = os.path.join(_base_dir(), "config.py")

# config.py に保存された言語を優先し、未設定・不正な値の場合は Windows の表示言語を使う
LANG = config.LANGUAGE if config.LANGUAGE in SUPPORTED_LANGS else _detect_lang()
_TABLE = _load_table(LANG)
_FALLBACK = _load_table(DEFAULT_LANG)

def t(key, **kwargs):
    """キーに対応する文言を返す。無い場合は英語版、それも無ければキー名を返す"""
    text = _TABLE.get(key) or _FALLBACK.get(key) or key
    return text.format(**kwargs) if kwargs else text

def set_language(lang):
    """表示言語を切り替える（文言テーブルを読み直す）"""
    global LANG, _TABLE
    LANG = lang
    _TABLE = _load_table(lang)

def save_language(lang):
    """選択された言語を config.py の LANGUAGE に書き込んで保存する"""
    with open(CONFIG_PATH, "r", encoding="utf-8") as f:
        text = f.read()
    line = f'LANGUAGE = "{lang}"'
    new_text, count = re.subn(r'^LANGUAGE = .*$', line, text, count=1, flags=re.MULTILINE)
    if count == 0:
        new_text = text.rstrip("\n") + "\n" + line + "\n"
    with open(CONFIG_PATH, "w", encoding="utf-8") as f:
        f.write(new_text)
    config.LANGUAGE = lang

def seg_key(text, keys):
    """CTkSegmentedButton で選択中の表示文字列から内部キーを返す"""
    return next((k for k in keys if t(k) == text), keys[0])

# セグメントボタンの選択肢（内部キーの並び）
CLICK_BUTTONS = ["left_click", "right_click"]
DRAG_BUTTONS = ["left_drag", "right_drag"]
HOLD_BUTTONS = ["left_hold", "right_hold"]
KEYBOARD_MODES = ["text", "hotkey"]
DRAG_TARGETS = ["start", "end", "via"]

# 日本語版で保存された .acf の値を内部キーに変換する表
LEGACY_VALUES = {
    "左クリック": "left_click", "右クリック": "right_click",
    "左ドラッグ": "left_drag", "右ドラッグ": "right_drag",
    "左長押し": "left_hold", "右長押し": "right_hold",
    "入力モード": "text", "ホットキー": "hotkey",
}
