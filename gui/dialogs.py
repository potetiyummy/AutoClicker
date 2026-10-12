import customtkinter as ctk
from i18n import t_in

def ask_unsupported_system(parent, lang):
    """システムの規定が対応外のときの確認。英語を使う場合は True、他の言語に変更する場合は False を返す。
    メッセージは lang（既定になる前の表示言語）で表示する"""
    choice = {"use_english": False}

    win = ctk.CTkToplevel(parent)
    win.title(t_in(lang, "info_title"))
    win.resizable(False, False)
    win.transient(parent)

    ctk.CTkLabel(win, text="ℹ", font=("Yu Gothic UI", 32, "bold")).pack(pady=(20, 0))
    ctk.CTkLabel(win, text=t_in(lang, "sys_unsupported_msg"), wraplength=380, justify="left",
                 font=("Yu Gothic UI", 13, "normal")).pack(padx=24, pady=12)

    def use_english():
        choice["use_english"] = True
        win.destroy()

    buttons = ctk.CTkFrame(win, fg_color="transparent")
    buttons.pack(pady=(0, 18))
    ctk.CTkButton(buttons, text=t_in(lang, "btn_use_english"), font=("Yu Gothic UI", 12, "bold"),
                  command=use_english).pack(side="left", padx=8)
    ctk.CTkButton(buttons, text=t_in(lang, "btn_change_language"), font=("Yu Gothic UI", 12, "bold"),
                  fg_color="#555555", command=win.destroy).pack(side="left", padx=8)

    win.protocol("WM_DELETE_WINDOW", win.destroy)
    win.after(50, win.grab_set)
    win.wait_window()
    return choice["use_english"]
