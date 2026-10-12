import customtkinter as ctk
from i18n import t, HOLD_BUTTONS
from ..waypoint_list import WaypointListCanvas

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

    # 経由地（長押しを続けたまま通る座標）
    fhw = ctk.CTkFrame(th, fg_color="transparent")
    fhw.pack(pady=2)
    app.hw_x = ctk.CTkEntry(fhw, width=60, placeholder_text=t("via_x"), font=("Yu Gothic UI", 12, "normal"))
    app.hw_x.pack(side="left", padx=2)
    app.hw_y = ctk.CTkEntry(fhw, width=60, placeholder_text="Y", font=("Yu Gothic UI", 12, "normal"))
    app.hw_y.pack(side="left", padx=2)
    app.hw_pick_btn = ctk.CTkButton(fhw, text=t("pick_short"), font=("Yu Gothic UI", 11, "normal"), width=70, fg_color="#2B2B2B", command=lambda: app.start_pick("hold_via"))
    app.hw_pick_btn.pack(side="left", padx=4)

    fhw_btns = ctk.CTkFrame(th, fg_color="transparent")
    fhw_btns.pack(pady=2, fill="x", padx=10)
    ctk.CTkButton(fhw_btns, text=t("add_waypoint"), font=("Yu Gothic UI", 11, "bold"), fg_color="#1F6AA5", command=app.add_hold_waypoint_to_list).pack(side="left", padx=2)
    ctk.CTkButton(fhw_btns, text=t("clear_waypoints"), font=("Yu Gothic UI", 11, "normal"), fg_color="#555555", width=70, command=app.clear_hold_waypoints).pack(side="right", padx=2)

    # 長押しリスト（経由地の一覧）
    ctk.CTkLabel(th, text=t("hold_list_title"), font=("Yu Gothic UI", 11, "normal"), text_color="gray").pack(anchor="w", padx=15)
    app.hold_list = WaypointListCanvas(th, app.hold_waypoints)
    app.hold_list.pack(fill="x", padx=15, pady=2)

    ctk.CTkButton(th, text=t("add_hold"), font=("Yu Gothic UI", 13, "bold"), command=app.add_hold).pack(pady=5)
