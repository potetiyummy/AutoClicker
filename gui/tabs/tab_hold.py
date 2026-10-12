import customtkinter as ctk

def create_hold_tab(tabview, app):
    th = tabview.tab("長押し")
    app.h_btn = ctk.CTkSegmentedButton(th, values=["左長押し", "右長押し"], font=("Yu Gothic UI", 12, "normal"))
    app.h_btn.set("左長押し")
    app.h_btn.pack(pady=5)
    
    fh = ctk.CTkFrame(th, fg_color="transparent")
    fh.pack(pady=2)
    app.h_x = ctk.CTkEntry(fh, width=50, placeholder_text="X", font=("Yu Gothic UI", 12, "normal"))
    app.h_x.pack(side="left", padx=2)
    app.h_y = ctk.CTkEntry(fh, width=50, placeholder_text="Y", font=("Yu Gothic UI", 12, "normal"))
    app.h_y.pack(side="left", padx=2)
    app.h_dur = ctk.CTkEntry(fh, width=80, placeholder_text="ミリ秒", font=("Yu Gothic UI", 12, "normal"))
    app.h_dur.pack(side="left", padx=2)
    
    app.h_pick_btn = ctk.CTkButton(th, text="座標を取得する", font=("Yu Gothic UI", 12, "normal"), fg_color="#2B2B2B", command=lambda: app.start_pick("hold"))
    app.h_pick_btn.pack(pady=2)
    ctk.CTkButton(th, text="長押しを追加", font=("Yu Gothic UI", 13, "bold"), command=app.add_hold).pack(pady=5)
