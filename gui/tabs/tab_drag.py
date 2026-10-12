import customtkinter as ctk

class DragTabManager:
    def __init__(self, tabview, app):
        self.app = app
        self.td = tabview.tab("ドラッグ")
        self.setup_ui()

    def setup_ui(self):
        self.app.d_btn = ctk.CTkSegmentedButton(self.td, values=["左ドラッグ", "右ドラッグ"], font=("Yu Gothic UI", 12, "normal"))
        self.app.d_btn.set("左ドラッグ")
        self.app.d_btn.pack(pady=3)
        
        fd_target = ctk.CTkFrame(self.td, fg_color="transparent")
        fd_target.pack(pady=2)
        self.app.d_target_mode = ctk.CTkSegmentedButton(fd_target, values=["始点", "終点", "新規経由地"], font=("Yu Gothic UI", 11, "normal"))
        self.app.d_target_mode.set("始点")
        self.app.d_target_mode.pack(side="left", padx=5)
        self.app.d_pick_btn = ctk.CTkButton(fd_target, text="座標取得", font=("Yu Gothic UI", 11, "normal"), width=70, fg_color="#2B2B2B", command=lambda: self.app.start_pick("drag"))
        self.app.d_pick_btn.pack(side="left", padx=5)

        fd = ctk.CTkFrame(self.td, fg_color="transparent")
        fd.pack(pady=2)
        self.app.d_x = ctk.CTkEntry(fd, width=60, placeholder_text="X", font=("Yu Gothic UI", 12, "normal"))
        self.app.d_x.insert(0, "0")
        self.app.d_x.pack(side="left", padx=4)
        self.app.d_y = ctk.CTkEntry(fd, width=60, placeholder_text="Y", font=("Yu Gothic UI", 12, "normal"))
        self.app.d_y.insert(0, "0")
        self.app.d_y.pack(side="left", padx=4)

        fd_fixed = ctk.CTkFrame(self.td, fg_color="transparent")
        fd_fixed.pack(pady=2)
        self.app.lbl_start = ctk.CTkLabel(fd_fixed, text="始点: (0, 0)", font=("Yu Gothic UI", 11, "normal"), text_color="gray")
        self.app.lbl_start.pack(side="left", padx=10)
        self.app.lbl_end = ctk.CTkLabel(fd_fixed, text="終点: (100, 100)", font=("Yu Gothic UI", 11, "normal"), text_color="gray")
        self.app.lbl_end.pack(side="left", padx=10)

        fd_way = ctk.CTkFrame(self.td, fg_color="transparent")
        fd_way.pack(pady=2, fill="x", padx=10)
        
        self.app.add_way_btn = ctk.CTkButton(fd_way, text="経由地を追加", font=("Yu Gothic UI", 11, "bold"), fg_color="#1F6AA5", command=self.app.add_waypoint_to_list)
        self.app.add_waybtn = self.app.add_way_btn # セーフティ用リファレンス
        self.app.add_way_btn.pack(side="left", padx=2)
        
        self.app.clear_way_btn = ctk.CTkButton(fd_way, text="経由クリア", font=("Yu Gothic UI", 11, "normal"), fg_color="#555555", width=70, command=self.app.clear_waypoints)
        self.app.clear_way_btn.pack(side="right", padx=2)

        self.app.way_listbox = ctk.CTkTextbox(self.td, height=50, font=("Yu Gothic UI", 11, "normal"), fg_color="#202020", activate_scrollbars=True)
        self.app.way_listbox.pack(fill="x", padx=15, pady=2)
        self.app.update_waypoint_display()

        ctk.CTkButton(self.td, text="ドラッグマクロを追加", font=("Yu Gothic UI", 13, "bold"), command=self.app.add_drag).pack(pady=5)
