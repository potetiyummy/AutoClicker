import time
from tkinter import messagebox
from pynput.mouse import Button, Controller as MouseController
from pynput.keyboard import Controller as KeyboardController, Key
from key_map import KEY_MAP
from i18n import t

mouse = MouseController()
keyboard = KeyboardController()

class MacroEngine:
    def __init__(self, app_ui=None):
        self.ui = app_ui
        self.actions = []
        self.active = False
        self.running = True

    def set_ui(self, app_ui):
        self.ui = app_ui

    def is_valid_coordinate(self, x, y):
        """指定された(x, y)座標が現在のパソコンの画面内に収まっているか判定する"""
        if self.ui:
            # 現在のPCモニターの横幅と縦幅の最大ピクセル数をリアルタイム取得
            screen_width = self.ui.winfo_screenwidth()
            screen_height = self.ui.winfo_screenheight()
            
            # 0未満、または画面サイズを超えている場合は無効とみなす
            if x < 0 or x >= screen_width or y < 0 or y >= screen_height:
                return False
        return True

    def check_all_coordinates(self):
        """登録されているすべてのアクションの座標が安全かチェックする"""
        for act in self.actions:
            t = act["type"]
            
            # 1. クリック、長押しの座標チェック
            if t in ["click", "hold"]:
                if not self.is_valid_coordinate(act["x"], act["y"]):
                    return False
                    
            # 2. ドラッグ（始点、終点、すべての経由地）の座標チェック
            elif t == "drag":
                # 始点と終点のチェック
                if not self.is_valid_coordinate(act["sx"], act["sy"]): return False
                if not self.is_valid_coordinate(act["ex"], act["ey"]): return False
                
                # 追加されたすべての経由地のチェック
                if "waypoints" in act and act["waypoints"]:
                    for wp in act["waypoints"]:
                        if not self.is_valid_coordinate(wp[0], wp[1]):
                            return False
        return True

    def toggle(self):
        # 1. アクションリストに何も入っていない場合のガード処理
        if not self.actions:
            self.active = False
            if self.ui: self.ui.update_status(False)
            messagebox.showinfo(t("notice"), t("no_actions"))
            return

        # 2. 【追加】無効な画面外座標が含まれている場合のガード処理
        if not self.check_all_coordinates():
            self.active = False
            if self.ui: self.ui.update_status(False)
            # エラーモード(showerror)で指定された通りのエラー文を表示
            messagebox.showerror(t("error"), t("invalid_coord"))
            return

        # すべての安全チェックを通過したら通常通り実行開始
        self.active = not self.active
        if self.ui:
            self.ui.update_status(self.active)

    def parse_hotkey(self, hk_str):
        hk_str = hk_str.lower().strip()
        if hk_str.startswith("{") and hk_str.endswith("}"):
            hk_str = hk_str[1:-1]
            
        parts = hk_str.split('+')
        keys = []
        for p in parts:
            p = p.strip()
            bracket_p = f"{{{p}}}"
            if bracket_p in KEY_MAP:
                keys.append(KEY_MAP[bracket_p])
            else:
                if len(p) == 1:
                    keys.append(p)
                elif hasattr(Key, p):
                    keys.append(getattr(Key, p))
                else:
                    keys.append(p)
        return keys

    def loop(self):
        while self.running:
            if self.active and self.actions:
                for act in self.actions:
                    if not self.active:
                        break
                    
                    t = act["type"]
                    
                    if t == "click":
                        btn = Button.right if "right" in act["btn"] else Button.left
                        mouse.position = (act["x"], act["y"])
                        time.sleep(0.05)
                        mouse.click(btn, 1)

                    elif t == "drag":
                        btn = Button.right if "right" in act["btn"] else Button.left
                        mouse.position = (act["sx"], act["sy"])
                        time.sleep(0.05)
                        mouse.press(btn)
                        time.sleep(0.05)
                        
                        if "waypoints" in act and act["waypoints"]:
                            for wp in act["waypoints"]:
                                mouse.position = (wp[0], wp[1]) # インデックス指定バグを修正
                                time.sleep(0.1)
                        
                        mouse.position = (act["ex"], act["ey"])
                        time.sleep(0.05)
                        mouse.release(btn)

                    elif t == "hold":
                        btn = Button.right if "right" in act["btn"] else Button.left
                        mouse.position = (act["x"], act["y"])
                        time.sleep(0.05)
                        mouse.press(btn)
                        time.sleep(act["duration"] / 1000.0)
                        mouse.release(btn)

                    elif t == "keyboard":
                        if act["mode"] == "text":
                            keyboard.type(act["value"])
                        else:
                            try:
                                k_list = self.parse_hotkey(act["value"])
                                for k in k_list:
                                    if k == "fn": continue
                                    keyboard.press(k)
                                time.sleep(0.05)
                                for k in reversed(k_list):
                                    if k == "fn": continue
                                    keyboard.release(k)
                            except:
                                pass

                    elif t == "wait":
                        time.sleep(act["duration"] / 1000.0)
                        
                    time.sleep(0.1)
            else:
                time.sleep(0.2)
