import customtkinter as ctk
from i18n import t, CLICK_BUTTONS

def create_click_tab(tabview, app):
    tc = tabview.tab(t("click"))
    app.c_btn = ctk.CTkSegmentedButton(tc, values=[t(k) for k in CLICK_BUTTONS], font=("Yu Gothic UI", 12, "normal"))
    app.c_btn.set(t("left_click"))
    app.c_btn.pack(pady=5)

    fc = ctk.CTkFrame(tc, fg_color="transparent")
    fc.pack(pady=5)
    app.c_x = ctk.CTkEntry(fc, width=60, font=("Yu Gothic UI", 13, "normal"))
    app.c_x.insert(0, "0")
    app.c_x.pack(side="left", padx=5)
    app.c_y = ctk.CTkEntry(fc, width=60, font=("Yu Gothic UI", 13, "normal"))
    app.c_y.insert(0, "0")
    app.c_y.pack(side="left", padx=5)

    app.c_pick_btn = ctk.CTkButton(tc, text=t("pick"), font=("Yu Gothic UI", 12, "normal"), fg_color="#2B2B2B", command=lambda: app.start_pick("click"))
    app.c_pick_btn.pack(pady=2)
    ctk.CTkButton(tc, text=t("add_click"), font=("Yu Gothic UI", 13, "bold"), command=app.add_click).pack(pady=5)
