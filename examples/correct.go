package main

import "fmt"
func main() {
    var userName string = "Alex\n"
    count := 1_000
    hex := 0xFF
    bin := 0b1010
    oct := 0o755
    pi := 3.14
    small := .25
    ten := 10.
    exp := 1.5e-3
    z := 10i
    r := '\n'
    raw := `raw string`
    fmt.Println(userName, count, hex, bin, oct, pi, small, ten, exp, z, r, raw)
}
