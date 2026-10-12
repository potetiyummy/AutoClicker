import json
from tkinter import filedialog

class FileManager:
    def __init__(self, engine, app_ui):
        self.engine = engine
        self.ui = app_ui

    def save_acf(self):
        p = filedialog.asksaveasfilename(defaultextension=".acf", filetypes=[("AutoClicker Config", "*.acf")])
        if p:
            with open(p, "w", encoding="utf-8") as f:
                json.dump(self.engine.actions, f, ensure_ascii=False, indent=4)

    def load_acf(self):
        p = filedialog.askopenfilename(
            title="acfをインポート", 
            filetypes=[("AutoClicker Config", "*.acf")]
        )
        if p:
            with open(p, "r", encoding="utf-8") as f:
                self.engine.actions = json.load(f)
            self.ui.refresh_list()
