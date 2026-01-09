import sys
import ctypes
import time

def check_and_handle_popup(target_title: str):
    """
    Checks for a visible window with 'target_title'.
    If found, attempts to click the 'Save' button.
    """
    user32 = ctypes.windll.user32
    hwnd_target = None
    
    # 1. Find the window
    def foreach_window(hwnd, lParam):
        nonlocal hwnd_target
        if user32.IsWindowVisible(hwnd):
            length = user32.GetWindowTextLengthW(hwnd)
            if length > 0:
                buff = ctypes.create_unicode_buffer(length + 1)
                user32.GetWindowTextW(hwnd, buff, length + 1)
                title = buff.value
                if target_title.lower() in title.lower():
                    hwnd_target = hwnd
                    return False # Found, stop enumeration
        return True

    ENUM_WINDOWS_PROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)
    user32.EnumWindows(ENUM_WINDOWS_PROC(foreach_window), 0)

    if not hwnd_target:
        print("No 'save scene popup detected'")
        sys.exit(0)

    # 2. Popup Found. Try to find "Save" button.
    found_save_btn = None
    
    def foreach_child(hwnd, lParam):
        nonlocal found_save_btn
        length = user32.GetWindowTextLengthW(hwnd)
        if length > 0:
            buff = ctypes.create_unicode_buffer(length + 1)
            user32.GetWindowTextW(hwnd, buff, length + 1)
            text = buff.value
            # Check for "Save" or "&Save" (accelerator)
            if text == "Save" or text == "&Save":
                found_save_btn = hwnd
                return False
        return True

    ENUM_CHILD_PROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)
    user32.EnumChildWindows(hwnd_target, ENUM_CHILD_PROC(foreach_child), 0)

    if found_save_btn:
        # BM_CLICK = 0x00F5
        user32.SendMessageW(found_save_btn, 0x00F5, 0, 0)
        print("Clicked 'Save' option in the popup.")
        time.sleep(0.5) 
        sys.exit(0)
    else:
        # Fallback: Send Enter if specific button not found (often defaults to Save)
        user32.SetForegroundWindow(hwnd_target)
        time.sleep(0.1)
        # VK_RETURN = 0x0D
        user32.keybd_event(0x0D, 0, 0, 0) # Key down
        user32.keybd_event(0x0D, 0, 2, 0) # Key up
        print("Clicked 'Save' (via Enter) in the popup.")
        time.sleep(0.5)
        sys.exit(0)

if __name__ == "__main__":
    search = "Scene(s) Have Been Modified"
    if len(sys.argv) > 1:
        search = " ".join(sys.argv[1:])
    
    check_and_handle_popup(search)
