import customtkinter as ctk
from pynput.mouse import Controller as MouseController
from i18n import t, seg_key, CLICK_BUTTONS, DRAG_BUTTONS, HOLD_BUTTONS, KEYBOARD_MODES, DRAG_TARGETS

mouse_ctrl = MouseController()

class ActionEventHandler:
    def __init__(self, app):
        self.app = app

    def start_pick(self, tab_type):
        self.countdown(3, tab_type)

    def countdown(self, count, tab_type):
        btn = {"click": self.app.c_pick_btn, "drag": self.app.d_pick_btn, "hold": self.app.h_pick_btn}.get(tab_type)
        if count > 0:
            if btn: btn.configure(text=t("countdown", count=count), fg_color="#A80000")
            self.app.after(1000, lambda: self.countdown(count - 1, tab_type))
        else:
            if btn: btn.configure(text=t("pick") if tab_type != "drag" else t("pick_short"), fg_color="#2B2B2B")
            x, y = mouse_ctrl.position
            if tab_type == "click":
                self.app.c_x.delete(0, "end"); self.app.c_x.insert(0, str(x))
                self.app.c_y.delete(0, "end"); self.app.c_y.insert(0, str(y))
            elif tab_type == "hold":
                self.app.h_x.delete(0, "end"); self.app.h_x.insert(0, str(x))
                self.app.h_y.delete(0, "end"); self.app.h_y.insert(0, str(y))
            elif tab_type == "drag":
                self.app.d_x.delete(0, "end"); self.app.d_x.insert(0, str(x))
                self.app.d_y.delete(0, "end"); self.app.d_y.insert(0, str(y))
                mode = seg_key(self.app.d_target_mode.get(), DRAG_TARGETS)
                if mode == "start":
                    self.app.drag_start_coord = [x, y]
                    self.app.lbl_start.configure(text=t("start_label", x=x, y=y))
                elif mode == "end":
                    self.app.drag_end_coord = [x, y]
                    self.app.lbl_end.configure(text=t("end_label", x=x, y=y))

    def add_click(self):
        self.app.engine.actions.append({"type": "click", "btn": seg_key(self.app.c_btn.get(), CLICK_BUTTONS), "x": int(self.app.c_x.get() or 0), "y": int(self.app.c_y.get() or 0)})
        self.app.refresh_list()

    def add_drag(self):
        self.app.engine.actions.append({
            "type": "drag", "btn": seg_key(self.app.d_btn.get(), DRAG_BUTTONS),
            "sx": self.app.drag_start_coord, "sy": self.app.drag_start_coord,
            "waypoints": list(self.app.current_waypoints),
            "ex": self.app.drag_end_coord, "ey": self.app.drag_end_coord
        })
        self.app.refresh_list()

    def add_hold(self):
        self.app.engine.actions.append({"type": "hold", "btn": seg_key(self.app.h_btn.get(), HOLD_BUTTONS), "x": int(self.app.h_x.get() or 0), "y": int(self.app.h_y.get() or 0), "duration": int(self.app.h_dur.get() or 1000)})
        self.app.refresh_list()

    def add_kb(self):
        self.app.engine.actions.append({"type": "keyboard", "mode": seg_key(self.app.k_mod.get(), KEYBOARD_MODES), "value": self.app.k_val.get()})
        self.app.refresh_list()

    def add_wait(self):
        self.app.engine.actions.append({"type": "wait", "duration": int(self.app.w_dur.get() or 500)})
        self.app.refresh_list()
