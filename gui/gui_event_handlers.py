import customtkinter as ctk
from pynput.mouse import Controller as MouseController

mouse_ctrl = MouseController()

class ActionEventHandler:
    def __init__(self, app):
        self.app = app

    def start_pick(self, tab_type):
        self.countdown(3, tab_type)

    def countdown(self, count, tab_type):
        btn = {"click": self.app.c_pick_btn, "drag": self.app.d_pick_btn, "hold": self.app.h_pick_btn}.get(tab_type)
        if count > 0:
            if btn: btn.configure(text=f"{count}秒後に取得...", fg_color="#A80000")
            self.app.after(1000, lambda: self.countdown(count - 1, tab_type))
        else:
            if btn: btn.configure(text="座標を取得する" if tab_type != "drag" else "座標取得", fg_color="#2B2B2B")
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
                mode = self.app.d_target_mode.get()
                if mode == "始点":
                    self.app.drag_start_coord = [x, y]
                    self.app.lbl_start.configure(text=f"始点: ({x}, {y})")
                elif mode == "終点":
                    self.app.drag_end_coord = [x, y]
                    self.app.lbl_end.configure(text=f"終点: ({x}, {y})")

    def add_click(self):
        self.app.engine.actions.append({"type": "click", "btn": self.app.c_btn.get(), "x": int(self.app.c_x.get() or 0), "y": int(self.app.c_y.get() or 0)})
        self.app.refresh_list()

    def add_drag(self):
        self.app.engine.actions.append({
            "type": "drag", "btn": self.app.d_btn.get(), 
            "sx": self.app.drag_start_coord, "sy": self.app.drag_start_coord, 
            "waypoints": list(self.app.current_waypoints), 
            "ex": self.app.drag_end_coord, "ey": self.app.drag_end_coord
        })
        self.app.refresh_list()

    def add_hold(self):
        self.app.engine.actions.append({"type": "hold", "btn": self.app.h_btn.get(), "x": int(self.app.h_x.get() or 0), "y": int(self.app.h_y.get() or 0), "duration": int(self.app.h_dur.get() or 1000)})
        self.app.refresh_list()

    def add_kb(self):
        self.app.engine.actions.append({"type": "keyboard", "mode": self.app.k_mod.get(), "value": self.app.k_val.get()})
        self.app.refresh_list()

    def add_wait(self):
        self.app.engine.actions.append({"type": "wait", "duration": int(self.app.w_dur.get() or 500)})
        self.app.refresh_list()
