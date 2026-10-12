import os
import sys
import tkinter as tk
import ctypes
import customtkinter as ctk
import i18n
from i18n import t, seg_key, KEYBOARD_MODES, LANGUAGE_NAMES

from .tabs import create_click_tab, DragTabManager, create_hold_tab, KeyboardTabManager, create_wait_tab
from .gui_io import FileManager
from .list_canvas import MacroListCanvas
from .gui_image_loader import ImageLoader          # 新規モジュール
from .gui_event_handlers import ActionEventHandler # 新規モジュール

ES_CONTINUOUS = 0x80000000
ES_SYSTEM_REQUIRED = 0x00000001
ES_DISPLAY_REQUIRED = 0x00000002

class AutoClickerApp(ctk.CTk):
    def __init__(self, engine):
        super().__init__()
        self.engine = engine
        self.title("Auto Clicker v1.5")
        
        # ウィンドウ用アイコン自動絶対パス指定
        icon_name = "app_icon.ico"
        if getattr(sys, 'frozen', False):
            base_path = sys._MEIPASS
            icon_path = os.path.join(base_path, icon_name)
        else:
            root_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
            icon_path = os.path.join(root_dir, icon_name)
        if os.path.exists(icon_path):
            self.iconbitmap(icon_path)
            
        self.geometry("820x630")
        self.resizable(False, False)
        
        # 各種外部マネージャー・ハンドラーの初期化
        self.io_manager = FileManager(self.engine, self)
        self.handler = ActionEventHandler(self)
        
        # 各サブモジュールと共有するデータ変数
        self.current_waypoints = []
        self.drag_start_coord = [0, 0]
        self.drag_end_coord = [100, 100]
        
        # 分離した画像ローダーからアセットを一括読込
        self.img_cache = ImageLoader.load_action_images()
        
        self.setup_ui()
        self.protocol("WM_DELETE_WINDOW", self.on_closing)

    def setup_ui(self):
        self.lang_menu = ctk.CTkOptionMenu(self, values=list(LANGUAGE_NAMES.keys()), width=120, command=self.on_language_changed)
        self.lang_menu.set(next(name for name, code in LANGUAGE_NAMES.items() if code == i18n.LANG))
        self.lang_menu.pack(anchor="ne", padx=20, pady=(10, 0))

        self.main_frame = ctk.CTkFrame(self, fg_color="transparent")
        self.main_frame.pack(fill="both", expand=True, padx=20, pady=20)

        # 左側エリア（操作パネル）
        self.left_frame = ctk.CTkFrame(self.main_frame, fg_color="transparent")
        self.left_frame.pack(side="left", fill="both", expand=True, padx=(0, 10))

        ctk.CTkLabel(self.left_frame, text=t("panel_title"), font=("Yu Gothic UI", 20, "bold")).pack(anchor="w", pady=(0, 10))

        self.tabview = ctk.CTkTabview(self.left_frame, height=340)
        self.tabview.pack(fill="x", pady=(0, 10))

        for key in ["click", "drag", "hold", "keyboard", "wait"]:
            self.tabview.add(t(key))
        self.tabview._segmented_button.configure(font=("Yu Gothic UI", 13, "normal"))
        
        # 各タブUIの組み立てとマウント
        create_click_tab(self.tabview, self)
        self.drag_manager = DragTabManager(self.tabview, self)
        create_hold_tab(self.tabview, self)
        self.kb_manager = KeyboardTabManager(self.tabview, self)
        create_wait_tab(self.tabview, self)

        self.start_btn = ctk.CTkButton(self.get_start_btn_master(), text=t("run"), font=("Yu Gothic UI", 14, "bold"), height=40, command=self.engine.toggle)
        self.start_btn.pack(fill="x", pady=5)
        
        io_f = ctk.CTkFrame(self.left_frame, fg_color="transparent")
        io_f.pack(fill="x")
        ctk.CTkButton(io_f, text=t("save_acf"), font=("Yu Gothic UI", 12, "bold"), command=self.io_manager.save_acf).pack(side="left", expand=True, fill="x", padx=2)
        ctk.CTkButton(io_f, text=t("load_acf"), font=("Yu Gothic UI", 12, "bold"), command=self.io_manager.load_acf).pack(side="right", expand=True, fill="x", padx=2)

        # 右側エリア（リストパネル）
        self.right_frame = ctk.CTkFrame(self.main_frame, fg_color="transparent")
        self.right_frame.pack(side="right", fill="both", expand=True, padx=(10, 0))
        ctk.CTkLabel(self.right_frame, text=t("list_title"), font=("Yu Gothic UI", 16, "bold")).pack(anchor="w")
        
        self.macro_list = MacroListCanvas(self.right_frame, self, self.engine)
        self.macro_list.pack(fill="both", expand=True, pady=5)
        
        self.bottom_status_frame = ctk.CTkFrame(self.right_frame, fg_color="transparent")
        self.bottom_status_frame.pack(fill="x", pady=(5, 0))
        
        self.status_label = ctk.CTkLabel(self.bottom_status_frame, text=t("status_stopped"), font=("Yu Gothic UI", 14, "bold"))
        self.status_label.pack(side="left", anchor="w")
        ctk.CTkButton(self.bottom_status_frame, text=t("clear"), font=("Yu Gothic UI", 12, "bold"), fg_color="#555555", width=70, command=self.clear_list).pack(side="right", anchor="e")

        self.keep_awake_var = tk.BooleanVar(value=False)
        self.keep_awake_check = ctk.CTkCheckBox(
            self.right_frame, 
            text=t("keep_awake"),
            variable=self.keep_awake_var,
            onvalue=True, offvalue=False,
            command=self.toggle_sleep_prevention,
            font=("Yu Gothic UI", 12, "normal")
        )
        self.keep_awake_check.pack(anchor="w", pady=(10, 0))

    def get_start_btn_master(self):
        return self.left_frame

    def on_language_changed(self, name):
        code = LANGUAGE_NAMES[name]
        if code == i18n.LANG:
            return
        # 選んだ言語は即座に config.py へ保存し、画面を新しい言語で組み直す
        i18n.save_language(code)
        i18n.set_language(code)
        self.rebuild_ui()

    def rebuild_ui(self):
        # 画面の再構築でチェックボックスの状態が消えないよう退避する
        keep_awake = self.keep_awake_var.get()
        for widget in self.winfo_children():
            widget.destroy()
        self.setup_ui()
        self.keep_awake_var.set(keep_awake)
        if keep_awake:
            self.toggle_sleep_prevention()
        self.refresh_list()

    # --- サブクラスへのイベント中継ブリッジメソッド ---
    def start_pick(self, tab_type): self.handler.start_pick(tab_type)
    def add_click(self): self.handler.add_click()
    def add_drag(self): self.handler.add_drag()
    def add_hold(self): self.handler.add_hold()
    def add_kb(self): self.handler.add_kb()
    def add_wait(self): self.handler.add_wait()

    def on_kb_mode_changed(self, mode):
        if seg_key(mode, KEYBOARD_MODES) == "text":
            self.k_val.configure(placeholder_text="Hello")
            self.validate_keyboard_input()
        else:
            self.k_val.configure(placeholder_text="{ctrl+c}")

    def validate_keyboard_input(self, *args):
        if seg_key(self.k_mod.get(), KEYBOARD_MODES) == "text":
            current_text = self.k_var.get()
            cleaned_text = current_text.replace("{", "").replace("}", "")
            if current_text != cleaned_text: self.k_var.set(cleaned_text)

    def add_waypoint_to_list(self): self.handler.app.add_waypoint_to_list_internal()
    def add_waypoint_to_list_internal(self):
        try:
            x = int(self.d_x.get() or 0)
            y = int(self.d_y.get() or 0)
            self.current_waypoints.append([x, y])
            self.update_waypoint_display()
        except ValueError: pass

    def clear_waypoints(self):
        self.current_waypoints.clear()
        self.update_waypoint_display()

    def update_waypoint_display(self):
        self.way_listbox.configure(state="normal")
        self.way_listbox.delete("1.0", "end")
        if not self.current_waypoints:
            self.way_listbox.insert("end", t("no_waypoints"))
        else:
            paths = [f"({wp[0]}, {wp[1]})" for wp in self.current_waypoints]
            self.way_listbox.insert("end", t("waypoint_order", path=" -> ".join(paths)))
        self.way_listbox.configure(state="disabled")

    def toggle_sleep_prevention(self):
        if self.keep_awake_var.get():
            try:
                ctypes.windll.kernel32.SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED)
                self.keep_awake_check.configure(text_color="#107C41") 
            except Exception: pass
        else: self.reset_sleep_state()

    def reset_sleep_state(self):
        try:
            ctypes.windll.kernel32.SetThreadExecutionState(ES_CONTINUOUS)
            self.keep_awake_check.configure(text_color="#F0F0F0")
        except Exception: pass

    def on_closing(self):
        self.reset_sleep_state()
        self.destroy()

    def refresh_list(self): self.macro_list.draw_list_items()
    def clear_list(self):
        self.engine.actions.clear()
        self.refresh_list()
