import math  # імпортуємо математичний модуль

def float_loop(a, n):
    """Функція для обчислення задачі з циклом"""
    res = 1
    # починаючи з нуля 1 до n включно
    for i in range(1, n+1):
        res *= (math.pow(a, 4) + 1) / (a - i)
    return res


# float_a, n = 1.234, 4    # задаємо значення

float_a = float(input("Enter a: "))
n = int(input("Enter n: "))

print("Using float: ", float_loop(float_a, n))
