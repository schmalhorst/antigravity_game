from PIL import Image, ImageDraw
import math

def create_vignette():
    width, height = 512, 512
    img = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    pixels = img.load()
    
    center_x, center_y = width / 2, height / 2
    max_radius = math.sqrt(center_x**2 + center_y**2) * 0.8
    
    for y in range(height):
        for x in range(width):
            dx = x - center_x
            dy = y - center_y
            distance = math.sqrt(dx*dx + dy*dy)
            
            # Normalize distance 0..1
            t = distance / max_radius
            
            # Smoothstep or power curve for vignette
            # We want clear center, red edges
            if t < 0.4:
                alpha = 0
            else:
                alpha = (t - 0.4) / 0.6
                alpha = alpha * alpha * alpha # Cubic for steeper falloff
                
            if alpha > 1: alpha = 1
            if alpha < 0: alpha = 0
            
            # Red color (255, 0, 0), alpha 0-255
            pixels[x, y] = (180, 0, 0, int(alpha * 255))
            
    img.save('Content/vignette.png')
    print("Generated Content/vignette.png")

if __name__ == "__main__":
    create_vignette()
