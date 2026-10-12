import customtkinter as ctk
from i18n import t, HOLD_BUTTONS

def create_hold_tab(tabview, app):
    th = tabview.tab(t("hold"))
    app.h_btn = ctk.CTkSegmentedButton(th, values=[t(k) for k in HOLD_BUTTONS], font=("Yu Gothic UI", 12, "normal"))
    app.h_btn.set(t("left_hold"))
    app.h_btn.pack(pady=5)

    fh = ctk.CTkFrame(th, fg_color="transparent")
    fh.pack(pady=2)
    app.h_x = ctk.CTkEntry(fh, width=50, placeholder_text="X", font=("Yu Gothic UI", 12, "normal"))
    app.h_x.pack(side="left", padx=2)
    app.h_y = ctk.CTkEntry(fh, width=50, placeholder_text="Y", font=("Yu Gothic UI", 12, "normal"))
    app.h_y.pack(side="left", padx=2)
    app.h_dur = ctk.CTkEntry(fh, width=80, placeholder_text=t("ms"), font=("Yu Gothic UI", 12, "normal"))
    app.h_dur.pack(side="left", padx=2)

    app.h_pick_btn = ctk.CTkButton(th, text=t("pick"), font=("Yu Gothic UI", 12, "normal"), fg_color="#2B2B2B", command=lambda: app.start_pick("hold"))
    app.h_pick_btn.pack(pady=2)
    ctk.CTkButton(th, text=t("add_hold"), font=("Yu Gothic UI", 13, "bold"), command=app.add_hold).pack(pady=5)
