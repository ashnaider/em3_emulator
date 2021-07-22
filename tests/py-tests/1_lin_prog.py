import math  # імпортуємо математичний модуль

def int_lin_prog(a, b):
    """Функція для обчислення лінійної програми"""
    return math.pow(a - 2*b, 3) / (b - 5)


float_a, float_b = 45.364, -8.32  # задаємо значення
# виводимо на екран результат обчислення
print("Using float: ", int_lin_prog(float_a, float_b))
