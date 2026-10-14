#include <stdio.h>
#include <stdlib.h>
#include <omp.h>

#define N 1000000

int main(void)
{
    int n = N;   /* variable, not macro - used in shared() */
    int *in  = malloc(n * sizeof(int));
    int *out = malloc(n * sizeof(int));

    if (!in || !out) { fprintf(stderr, "malloc failed\n"); return 1; }

    for (int i = 0; i < n; i++) in[i] = i % 100;

    int tmp;   /* shared by default - THE BUG */

#ifdef FIX_PRIVATE
    #pragma omp parallel for default(none) shared(in, out, n) private(tmp)
#elif defined(FIX_LOCAL)
    #pragma omp parallel for default(none) shared(in, out, n)
#elif defined(DEFAULT_NONE)
    #pragma omp parallel for default(none) shared(in, out, n)
#else
    #pragma omp parallel for
#endif
    for (int i = 0; i < n; i++)
    {
#ifdef FIX_LOCAL
        int tmp = in[i] * 3;
#else
        tmp = in[i] * 3;
#endif
        out[i] = tmp + 7;
    }

    int wrong = 0;
    for (int i = 0; i < n; i++)
    {
        int expected = (in[i] * 3) + 7;
        if (out[i] != expected) wrong++;
    }

    printf("Wrong elements: %d\n", wrong);

    free(in); free(out);
    return 0;
}
