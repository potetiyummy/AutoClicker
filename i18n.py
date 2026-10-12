import os
import re
import sys

DEFAULT_LANG = "en-US"
SUPPORTED_LANGS = ("ja-JP", "en-US", "ko-KR", "zh-CN", "zh-TW")
# 言語メニューで「システムの規定」を表す値（config.py では None として保存する）
SYSTEM = "system"
# メニューに表示する言語名（各言語の表記のまま。翻訳しない）
LANGUAGE_NAMES = {
    "日本語": "ja-JP",
    "English": "en-US",
    "한국어": "ko-KR",
    "简体中文": "zh-CN",
    "繁體中文": "zh-TW",
}

def _base_dir():
    if getattr(sys, 'frozen', False):
        return sys._MEIPASS
    return os.path.dirname(os.path.abspath(__file__))

def system_lang():
    """Windowsの表示言語に対応する言語コード。対応していない場合は None"""
    try:
        import ctypes
        langid = ctypes.windll.kernel32.GetUserDefaultUILanguage()
        primary, sub = langid & 0x3FF, langid >> 10
        if primary == 0x11:   # 日本語
            return "ja-JP"
        if primary == 0x09:   # 英語
            return "en-US"
        if primary == 0x12:   # 韓国語
            return "ko-KR"
        if primary == 0x04:   # 中国語: 簡体字(2)・シンガポール(4) は zh-CN、それ以外は繁体字
            return "zh-CN" if sub in (2, 4) else "zh-TW"
    except Exception:
        pass
    return None

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

def _saved_language():
    """config.py に保存された言語。ファイルが無い場合（クローン直後など）は None"""
    try:
        import config
        return getattr(config, "LANGUAGE", None)
    except ImportError:
        return None

_FALLBACK = _load_table(DEFAULT_LANG)
_tables = {}

# config.py に言語が保存されていればそれを使い、未設定・不正な値の場合はシステムの規定を使う
_saved = _saved_language()
SYSTEM_MODE = _saved not in SUPPORTED_LANGS
LANG = (system_lang() or DEFAULT_LANG) if SYSTEM_MODE else _saved
_TABLE = _load_table(LANG)

def t(key, **kwargs):
    """キーに対応する文言を返す。無い場合は英語版、それも無ければキー名を返す"""
    text = _TABLE.get(key) or _FALLBACK.get(key) or key
    return text.format(**kwargs) if kwargs else text

def t_in(lang, key, **kwargs):
    """指定した言語で文言を返す（現在の表示言語とは無関係に使う）"""
    if lang not in _tables:
        _tables[lang] = _load_table(lang)
    text = _tables[lang].get(key) or _FALLBACK.get(key) or key
    return text.format(**kwargs) if kwargs else text

def set_language(lang):
    """表示言語を切り替える（文言テーブルを読み直す）"""
    global LANG, _TABLE
    LANG = lang
    _TABLE = _load_table(lang)

def save_language(lang):
    """選択された言語を config.py の LANGUAGE に書き込む（None はシステムの規定）"""
    text = ""
    if os.path.exists(CONFIG_PATH):
        with open(CONFIG_PATH, "r", encoding="utf-8") as f:
            text = f.read()
    line = "LANGUAGE = None" if lang is None else f'LANGUAGE = "{lang}"'
    new_text, count = re.subn(r'^LANGUAGE = .*$', line, text, count=1, flags=re.MULTILINE)
    if count == 0:
        new_text = (text.rstrip("\n") + "\n" if text else "") + line + "\n"
    with open(CONFIG_PATH, "w", encoding="utf-8") as f:
        f.write(new_text)

def apply_language(choice):
    """言語を設定して保存する。choice は言語コードか SYSTEM"""
    global SYSTEM_MODE
    if choice == SYSTEM:
        SYSTEM_MODE = True
        save_language(None)
        set_language(system_lang() or DEFAULT_LANG)
    else:
        SYSTEM_MODE = False
        save_language(choice)
        set_language(choice)

def language_options():
    """言語メニューの項目 [(表示名, 言語コード)]。先頭はシステムの規定"""
    return [(t("system_default"), SYSTEM)] + list(LANGUAGE_NAMES.items())

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
