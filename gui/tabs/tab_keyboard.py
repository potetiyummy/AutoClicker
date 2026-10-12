import customtkinter as ctk

class KeyboardTabManager:
    def __init__(self, tabview, app):
        self.app = app
        self.tk_tab = tabview.tab("キーボード")
        self.setup_ui()

    def setup_ui(self):
        self.app.k_mod = ctk.CTkSegmentedButton(self.tk_tab, values=["入力モード", "ホットキー"], font=("Yu Gothic UI", 12, "normal"))
        self.app.k_mod.set("入力モード")
        self.app.k_mod.pack(pady=5)
        self.app.k_mod.configure(command=self.app.on_kb_mode_changed)
        
        self.app.k_var = ctk.StringVar(value="Hello")
        self.app.k_var.trace_add("write", self.app.validate_keyboard_input)
        
        self.app.k_val = ctk.CTkEntry(self.tk_tab, width=200, textvariable=self.app.k_var, font=("Yu Gothic UI", 13, "normal"))
        self.app.k_val.pack(pady=3)
        
        self.app.kb_guide_lbl = ctk.CTkLabel(
            self.tk_tab, 
            text="※入力モードでは {} などの特殊記号は使えません。\n続けて装飾キーを入力する場合は、後ろにホットキーを追加してください。",
            font=("Yu Gothic UI", 10, "normal"),
            text_color="#A8A8A8",
            justify="center"
        )
        self.app.kb_guide_lbl.pack(pady=2)
        
        ctk.CTkButton(self.tk_tab, text="キーボードを追加", font=("Yu Gothic UI", 13, "bold"), command=self.app.add_kb).pack(pady=5)
