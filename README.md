# em3_emulator
three-address computer emulator

You can do:
* arithmetic operations (+, -, \*, /)
* goto
* conditional goto depending on omega register (formed by arithmetic operations)
* copy from one register to another
* input number or array
* print number of array
* stop program

Program code can be saved or read from file.

You can run program in debug mode, evaluating each row manualy by "Step" button.
You also can see all registers, operands and next row.
  
### Example

#### Task


#### em3 program
![main screen](img/em3_example.png)

#### Python3 program
`import math 

def float_loop(a, n):
    res = 1
    for i in range(1, n+1):
        res *= (math.pow(a, 4) + 1) / (a - i)
    return res

float_a = float(input("Enter a: "))
n = int(input("Enter n: "))

print("Using float: ", float_loop(float_a, n))
`

#### Python3 result
![python3 resuls](img/py_res_test.png)
