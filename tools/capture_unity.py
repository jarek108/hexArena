import pygetwindow as gw
import pyautogui
from PIL import ImageGrab
import os
import time
import sys

def capture_unity(purpose="inspector_check"):
    target = "Unity 6."
    windows = [w for w in gw.getWindowsWithTitle('') if target in w.title]
    
    if not windows:
        print(f"Error: Window containing '{target}' not found.")
        return

    unity_win = windows[0]
    
    try:
        if unity_win.isMinimized:
            unity_win.restore()
        unity_win.activate()
        time.sleep(1) 
    except Exception as e:
        print(f"Warning: Could not activate window: {e}")
    
    # Ensure directory exists
    output_dir = "screenshots"
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)

    # Generate filename: purpose_YYYYMMDD_HHMMSS.png
    timestamp = time.strftime("%Y%m%d_%H%M%S")
    filename = f"{purpose}_{timestamp}.png"
    save_path = os.path.join(output_dir, filename)
    
    bbox = (unity_win.left, unity_win.top, unity_win.right, unity_win.bottom)
    screenshot = ImageGrab.grab(bbox=bbox, all_screens=True)
    screenshot.save(save_path)
    
    print(f"Captured: {os.path.abspath(save_path)}")

if __name__ == "__main__":
    purpose_arg = sys.argv[1] if len(sys.argv) > 1 else "inspector_check"
    capture_unity(purpose_arg)
