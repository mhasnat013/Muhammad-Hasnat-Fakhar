#include <stdio.h>
#include <omp.h>

static void workA(void) { volatile double s = 0; for (long i = 0; i < 300000000L; i++) s += i; }
static void workB(void) { volatile double s = 0; for (long i = 0; i < 200000000L; i++) s += i; }
static void workC(void) { volatile double s = 0; for (long i = 0; i < 100000000L; i++) s += i; }

int main(void)
{
    double t0 = omp_get_wtime();

    #pragma omp parallel sections
    {
        #pragma omp section
        {
            int tid = omp_get_thread_num();
            printf("Section A executed by thread %d\n", tid);
            workA();
        }

        #pragma omp section
        {
            int tid = omp_get_thread_num();
            printf("Section B executed by thread %d\n", tid);
            workB();
        }

        #pragma omp section
        {
            int tid = omp_get_thread_num();
            printf("Section C executed by thread %d\n", tid);
            workC();
        }
    }

    double t1 = omp_get_wtime();
    printf("Wall time = %.3f s\n", t1 - t0);
    return 0;
}
