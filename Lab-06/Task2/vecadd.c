#include <stdio.h>
#include <stdlib.h>
#include <omp.h>

#define N 5000000
#define PASSES 20

void vec_add(double *a, double *b, double *c, int n)
{
    #pragma omp parallel for default(none) shared(a, b, c, n)
    for (int i = 0; i < n; i++)
    {
        c[i] = a[i] + b[i];
    }
}

int main(void)
{
    double *a = malloc(N * sizeof(double));
    double *b = malloc(N * sizeof(double));
    double *c = malloc(N * sizeof(double));

    if (!a || !b || !c) { fprintf(stderr, "malloc failed\n"); return 1; }

    for (int i = 0; i < N; i++) { a[i] = 1.0; b[i] = 2.0; c[i] = 0.0; }

    double start = omp_get_wtime();
    for (int p = 0; p < PASSES; p++)
        vec_add(a, b, c, N);
    double end = omp_get_wtime();

    int correct = 1;
    for (int i = 0; i < N; i++)
    {
        if (c[i] != 3.0) { correct = 0; break; }
    }

    printf("Threads=%d  Time=%.2f ms  correct=%s\n",
           omp_get_max_threads(),
           (end - start) * 1000.0,
           correct ? "YES" : "NO");

    free(a); free(b); free(c);
    return 0;
}
