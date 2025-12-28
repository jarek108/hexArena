import sys
import os

def activate_windows_win32(target_title):
    import ctypes
    # Windows API constants
    SW_RESTORE = 9
    user32 = ctypes.windll.user32
    
    def foreach_window(hwnd, lParam):
        if user32.IsWindowVisible(hwnd):
            length = user32.GetWindowTextLengthW(hwnd)
            buff = ctypes.create_unicode_buffer(length + 1)
            user32.GetWindowTextW(hwnd, buff, length + 1)
            title = buff.value
            
            if target_title.lower() in title.lower():
                print(f"Activating: {title}")
                
                # Only restore if minimized (IsIconic) to avoid resizing/moving maximized windows
                if user32.IsIconic(hwnd):
                    user32.ShowWindow(hwnd, SW_RESTORE)
                
                # Bring to front
                user32.SetForegroundWindow(hwnd)
        return True

    enum_windows_proc = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)
    user32.EnumWindows(enum_windows_proc(foreach_window), 0)

def activate_windows_linux(target_title):
    import subprocess
    try:
        # Get list of windows: ID, Desktop, Machine, Title
        output = subprocess.check_output(["wmctrl", "-l"], stderr=subprocess.STDOUT).decode('utf-8')
        for line in output.splitlines():
            parts = line.split(None, 3)
            if len(parts) < 4: continue
            window_id = parts[0]
            title = parts[3]
            
            if target_title.lower() in title.lower():
                print(f"Activating: {title}")
                # -i uses window ID, -a activates
                subprocess.run(["wmctrl", "-i", "-a", window_id])
    except FileNotFoundError:
        print("Error: 'wmctrl' not found. Please install it (e.g., sudo apt install wmctrl).")
    except Exception as e:
        print(f"Error on Linux: {e}")

def activate_windows(target_title):
    if sys.platform == "win32":
        activate_windows_win32(target_title)
    elif sys.platform.startswith("linux"):
        activate_windows_linux(target_title)
    else:
        print(f"Unsupported platform: {sys.platform}")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        search_string = " ".join(sys.argv[1:])
        activate_windows(search_string)
    else:
        print("Usage: python windowActivator.py <search_string>")