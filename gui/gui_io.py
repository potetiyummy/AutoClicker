import json
from tkinter import filedialog
from i18n import t, LEGACY_VALUES

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
            title=t("import_acf_title"),
            filetypes=[("AutoClicker Config", "*.acf")]
        )
        if p:
            with open(p, "r", encoding="utf-8") as f:
                self.engine.actions = [self.migrate_action(a) for a in json.load(f)]
            self.ui.refresh_list()

    @staticmethod
    def migrate_action(action):
        """旧バージョン(日本語のまま保存された値)を内部キーに変換する"""
        for field in ("btn", "mode"):
            if field in action:
                action[field] = LEGACY_VALUES.get(action[field], action[field])
        return action
