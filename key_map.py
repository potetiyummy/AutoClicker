from pynput.keyboard import Key

# 特殊キーの文字列マッピング（大文字小文字の揺れを防ぐためすべて小文字で定義）
KEY_MAP = {
    # 修飾キー・基本特殊キー
    "{ctrl}": Key.ctrl,
    "{lctrl}": Key.ctrl_l,
    "{rctrl}": Key.ctrl_r,      # 右Ctrlに対応
    "{shift}": Key.shift,
    "{lshift}": Key.shift_l,
    "{rshift}": Key.shift_r,    # 右Shiftに対応
    "{alt}": Key.alt,
    "{lalt}": Key.alt_l,
    "{ralt}": Key.alt_r,
    "{win}": Key.cmd,           # Windowsキーに対応
    "{fn}": "fn",               # 環境依存キー（ハードウェア制御のため文字列として保持）

    # 操作系キー
    "{enter}": Key.enter,
    "{space}": Key.space,
    "{tab}": Key.tab,
    "{esc}": Key.esc,
    "{delete}": Key.delete,
    "{backspace}": Key.backspace,
    "{prtsc}": Key.print_screen, # プリントスクリーンに対応

    # 矢印キー
    "{left}": Key.left,
    "{right}": Key.right,
    "{up}": Key.up,
    "{down}": Key.down,

    # ファンクションキー (F1 〜 F12)
    "{f1}": Key.f1,   "{f2}": Key.f2,   "{f3}": Key.f3,   "{f4}": Key.f4,
    "{f5}": Key.f5,   "{f6}": Key.f6,   "{f7}": Key.f7,   "{f8}": Key.f8,
    "{f9}": Key.f9,   "{f10}": Key.f10, "{f11}": Key.f11, "{f12}": Key.f12
}
