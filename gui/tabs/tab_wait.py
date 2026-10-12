import customtkinter as ctk

def create_wait_tab(tabview, app):
    tw = tabview.tab("待機")
    ctk.CTkLabel(tw, text="待機時間 (ミリ秒):", font=("Yu Gothic UI", 13, "normal")).pack(pady=5)
    app.w_dur = ctk.CTkEntry(tw, width=100, placeholder_text="ミリ秒", font=("Yu Gothic UI", 13, "normal"))
    app.w_dur.insert(0, "500")
    app.w_dur.pack(pady=5)
    ctk.CTkButton(tw, text="待機を追加", font=("Yu Gothic UI", 13, "bold"), command=app.add_wait).pack(pady=10)
