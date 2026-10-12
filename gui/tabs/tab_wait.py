import customtkinter as ctk
from i18n import t

def create_wait_tab(tabview, app):
    tw = tabview.tab(t("wait"))
    ctk.CTkLabel(tw, text=t("wait_time"), font=("Yu Gothic UI", 13, "normal")).pack(pady=5)
    app.w_dur = ctk.CTkEntry(tw, width=100, placeholder_text=t("ms"), font=("Yu Gothic UI", 13, "normal"))
    app.w_dur.insert(0, "500")
    app.w_dur.pack(pady=5)
    ctk.CTkButton(tw, text=t("add_wait"), font=("Yu Gothic UI", 13, "bold"), command=app.add_wait).pack(pady=10)
