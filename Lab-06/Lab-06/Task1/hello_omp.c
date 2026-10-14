#include <stdio.h>
#include <omp.h>

int main(void)
{
    printf("Before parallel region: omp_get_num_threads() = %d\n",
           omp_get_num_threads());

#ifdef CLAUSE_THREADS
    #pragma omp parallel num_threads(CLAUSE_THREADS)
#else
    #pragma omp parallel
#endif
    {
        int id   = omp_get_thread_num();
        int team = omp_get_num_threads();
        printf("Hello from thread %d of %d\n", id, team);
    }

    printf("After parallel region: omp_get_num_threads() = %d\n",
           omp_get_num_threads());

    return 0;
}
