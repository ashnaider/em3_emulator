def int_array(arr):
    res = 0
    for i in range(0, len(arr), 2):
        res += int(arr[i] / 2)
    return res

int_arr = [4, 5, 6, 7]
print("Int arr: ", int_array(int_arr))

