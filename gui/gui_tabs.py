import customtkinter as ctk
from pynput.mouse import Controller as MouseController

mouse_ctrl = MouseController()

class ActionTabs(ctk.CTkTabview):
    def __init__(self, master, engine, refresh_callback):
        super().__init__(master, height=320)
        self.engine = engine
        self.refresh_list = refresh_callback
        
        for name in ["クリック", "ドラッグ", "長押し", "キーボード", "待機"]:
            self.add(name)
            self._segmented_button.configure(font=("Yu Gothic UI", 13, "normal"))
            
        self.create_click_tab()
        self.create_drag_tab()
        self.create_hold_tab()
        self.create_keyboard_tab()
        self.create_wait_tab()

    def create_click_tab(self):
        tc = self.tab("クリック")
        self.c_btn = ctk.CTkSegmentedButton(tc, values=["左クリック", "右クリック"], font=("Yu Gothic UI", 12, "normal"))
        self.c_btn.set("左クリック")
        self.c_btn.pack(pady=5)
        
        fc = ctk.CTkFrame(tc, fg_color="transparent")
        fc.pack(pady=5)
        self.c_x = ctk.CTkEntry(fc, width=60, font=("Yu Gothic UI", 13, "normal"))
        self.c_x.insert(0, "0")
        self.c_x.pack(side="left", padx=5)
        self.c_y = ctk.CTkEntry(fc, width=60, font=("Yu Gothic UI", 13, "normal"))
        self.c_y.insert(0, "0")
        self.c_y.pack(side="left", padx=5)
        
        self.c_pick_btn = ctk.CTkButton(tc, text="座標を取得する", font=("Yu Gothic UI", 12, "normal"), fg_color="#2B2B2B", command=lambda: self.start_pick("click"))
        self.c_pick_btn.pack(pady=2)
        ctk.CTkButton(tc, text="クリックを追加", font=("Yu Gothic UI", 13, "bold"), command=self.add_click).pack(pady=5)

    def create_drag_tab(self):
        td = self.tab("ドラッグ")
        self.d_btn = ctk.CTkSegmentedButton(td, values=["左ドラッグ", "右ドラッグ"], font=("Yu Gothic UI", 12, "normal"))
        self.d_btn.set("左ドラッグ")
        self.d_btn.pack(pady=5)
        
        fd_target = ctk.CTkFrame(td, fg_color="transparent")
        fd_target.pack(pady=2)
        self.d_target_mode = ctk.CTkSegmentedButton(fd_target, values=["始点", "経由", "終点"], font=("Yu Gothic UI", 11, "normal"))
        self.d_target_mode.set("始点")
        self.d_target_mode.pack(side="left", padx=5)
        self.d_pick_btn = ctk.CTkButton(fd_target, text="座標取得", font=("Yu Gothic UI", 11, "normal"), width=70, fg_color="#2B2B2B", command=lambda: self.start_pick("drag"))
        self.d_pick_btn.pack(side="left", padx=5)

        fd = ctk.CTkFrame(td, fg_color="transparent")
        fd.pack(pady=2)
        self.d_sx = ctk.CTkEntry(fd, width=50, placeholder_text="始点X", font=("Yu Gothic UI", 12, "normal"))
        self.d_sx.pack(side="left", padx=2)
        self.d_sy = ctk.CTkEntry(fd, width=50, placeholder_text="Y", font=("Yu Gothic UI", 12, "normal"))
        self.d_sy.pack(side="left", padx=2)
        self.d_mx = ctk.CTkEntry(fd, width=50, placeholder_text="経由X", font=("Yu Gothic UI", 12, "normal"))
        self.d_mx.pack(side="left", padx=2)
        self.d_my = ctk.CTkEntry(fd, width=50, placeholder_text="Y", font=("Yu Gothic UI", 12, "normal"))
        self.d_my.pack(side="left", padx=2)
        self.d_ex = ctk.CTkEntry(fd, width=50, placeholder_text="終点X", font=("Yu Gothic UI", 12, "normal"))
        self.d_ex.pack(side="left", padx=2)
        self.d_ey = ctk.CTkEntry(fd, width=50, placeholder_text="Y", font=("Yu Gothic UI", 12, "normal"))
        self.d_ey.pack(side="left", padx=2)
        ctk.CTkButton(td, text="ドラッグを追加", font=("Yu Gothic UI", 13, "bold"), command=self.add_drag).pack(pady=10)

    def create_hold_tab(self):
        th = self.tab("長押し")
        self.h_btn = ctk.CTkSegmentedButton(th, values=["左長押し", "右長押し"], font=("Yu Gothic UI", 12, "normal"))
        self.h_btn.set("左長押し")
        self.h_btn.pack(pady=5)
        
        fh = ctk.CTkFrame(th, fg_color="transparent")
        fh.pack(pady=2)
        self.h_x = ctk.CTkEntry(fh, width=50, placeholder_text="X", font=("Yu Gothic UI", 12, "normal"))
        self.h_x.pack(side="left", padx=2)
        self.h_y = ctk.CTkEntry(fh, width=50, placeholder_text="Y", font=("Yu Gothic UI", 12, "normal"))
        self.h_y.pack(side="left", padx=2)
        self.h_dur = ctk.CTkEntry(fh, width=80, placeholder_text="ミリ秒", font=("Yu Gothic UI", 12, "normal"))
        self.h_dur.pack(side="left", padx=2)
        
        self.h_pick_btn = ctk.CTkButton(th, text="座標を取得する", font=("Yu Gothic UI", 12, "normal"), fg_color="#2B2B2B", command=lambda: self.start_pick("hold"))
        self.h_pick_btn.pack(pady=2)
        ctk.CTkButton(th, text="長押しを追加", font=("Yu Gothic UI", 13, "bold"), command=self.add_hold).pack(pady=5)

    def create_keyboard_tab(self):
        tk_tab = self.tab("キーボード")
        self.k_mod = ctk.CTkSegmentedButton(tk_tab, values=["入力モード", "ホットキー"], font=("Yu Gothic UI", 12, "normal"))
        self.k_mod.set("入力モード")
        self.k_mod.pack(pady=5)
        self.k_val = ctk.CTkEntry(tk_tab, width=200, placeholder_text="Hello / {ctrl}+c", font=("Yu Gothic UI", 13, "normal"))
        self.k_val.pack(pady=5)
        ctk.CTkButton(tk_tab, text="キーボードを追加", font=("Yu Gothic UI", 13, "bold"), command=self.add_kb).pack(pady=10)

    def create_wait_tab(self):
        tw = self.tab("待機")
        self.w_dur = ctk.CTkEntry(tw, width=100, placeholder_text="ミリ秒", font=("Yu Gothic UI", 13, "normal"))
        self.w_dur.pack(pady=10)
        ctk.CTkButton(tw, text="待機を追加", font=("Yu Gothic UI", 13, "bold"), command=self.add_wait).pack(pady=10)

    def start_pick(self, tab_type):
        self.countdown(3, tab_type)

    def countdown(self, count, tab_type):
        btn = {"click": self.c_pick_btn, "drag": self.d_pick_btn, "hold": self.h_pick_btn}.get(tab_type)
        if count > 0:
            if btn: btn.configure(text=f"{count}秒後に取得...", fg_color="#A80000")
            self.after(1000, lambda: self.countdown(count - 1, tab_type))
        else:
            if btn: btn.configure(text="座標を取得する" if tab_type != "drag" else "座標取得", fg_color="#2B2B2B")
            x, y = mouse_ctrl.position
            if tab_type == "click":
                self.c_x.delete(0, "end"); self.c_x.insert(0, str(x))
                self.c_y.delete(0, "end"); self.c_y.insert(0, str(y))
            elif tab_type == "hold":
                self.h_x.delete(0, "end"); self.h_x.insert(0, str(x))
                self.h_y.delete(0, "end"); self.h_y.insert(0, str(y))
            elif tab_type == "drag":
                mode = self.d_target_mode.get()
                if mode == "始点":
                    self.d_sx.delete(0, "end"); self.d_sx.insert(0, str(x))
                    self.d_sy.delete(0, "end"); self.d_sy.insert(0, str(y))
                elif mode == "経由":
                    self.d_mx.delete(0, "end"); self.d_mx.insert(0, str(x))
                    self.d_my.delete(0, "end"); self.d_my.insert(0, str(y))
                elif mode == "終点":
                    self.d_ex.delete(0, "end"); self.d_ex.insert(0, str(x))
                    self.d_ey.delete(0, "end"); self.d_ey.insert(0, str(y))

    def add_click(self):
        self.engine.actions.append({"type": "click", "btn": self.c_btn.get(), "x": int(self.c_x.get() or 0), "y": int(self.c_y.get() or 0)})
        self.refresh_list()

    def add_drag(self):
        self.engine.actions.append({"type": "drag", "btn": self.d_btn.get(), "sx": int(self.d_sx.get() or 0), "sy": int(self.d_sy.get() or 0), "mx": int(self.d_mx.get()) if self.d_mx.get() else "", "my": int(self.d_my.get()) if self.d_my.get() else "", "ex": int(self.d_ex.get() or 0), "ey": int(self.d_ey.get() or 0)})
        self.refresh_list()

    def add_hold(self):
        self.engine.actions.append({"type": "hold", "btn": self.h_btn.get(), "x": int(self.h_x.get() or 0), "y": int(self.h_y.get() or 0), "duration": int(self.h_dur.get() or 1000)})
        self.refresh_list()

    def add_kb(self):
        self.engine.actions.append({"type": "keyboard", "mode": self.k_mod.get(), "value": self.k_val.get()})
        self.refresh_list()

    def add_wait(self):
        self.engine.actions.append({"type": "wait", "duration": int(self.w_dur.get() or 500)})
        self.refresh_list()
