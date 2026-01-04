import sys
import os

def focus_window_win32(target_title):
    import ctypes
    # Windows API constants
    SW_SHOW = 5      # Activates and displays in current size/pos
    SW_RESTORE = 9   # Activates and displays. If minimized, restores to original size/pos
    user32 = ctypes.windll.user32

    def foreach_window(hwnd, lParam):
        if user32.IsWindowVisible(hwnd) or user32.IsIconic(hwnd):
            length = user32.GetWindowTextLengthW(hwnd)
            buff = ctypes.create_unicode_buffer(length + 1)
            user32.GetWindowTextW(hwnd, buff, length + 1)
            title = buff.value

            if target_title.lower() in title.lower():
                print(f"Focusing: {title}")
                # If minimized (iconic), we must restore it to focus it
                if user32.IsIconic(hwnd):
                    user32.ShowWindow(hwnd, SW_RESTORE)
                else:
                    user32.ShowWindow(hwnd, SW_SHOW)
                
                user32.SetForegroundWindow(hwnd)
        return True

    enum_windows_proc = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)
    user32.EnumWindows(enum_windows_proc(foreach_window), 0)

def focus_window_linux(target_title):
    import subprocess
    try:
        output = subprocess.check_output(["wmctrl", "-l"], stderr=subprocess.STDOUT).decode('utf-8')
        for line in output.splitlines():
            parts = line.split(None, 3)
            if len(parts) < 4: continue
            window_id = parts[0]
            title = parts[3]

            if target_title.lower() in title.lower():
                print(f"Focusing: {title}")
                # -a activates the window by switching to its desktop and raising it
                subprocess.run(["wmctrl", "-i", "-a", window_id])
    except FileNotFoundError:
        print("Error: 'wmctrl' not found. Please install it (e.g., sudo apt install wmctrl).")
    except Exception as e:
        print(f"Error on Linux: {e}")

def focus_window(target_title):
    if sys.platform == "win32":
        focus_window_win32(target_title)
    elif sys.platform.startswith("linux"):
        focus_window_linux(target_title)
    else:
        print(f"Unsupported platform: {sys.platform}")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        search_string = " ".join(sys.argv[1:])
        focus_window(search_string)
    else:
        print("Usage: python newActivator.py <search_string>")
