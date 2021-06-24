using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Objects;

namespace Tests
{
    [TestClass]
    public class TaskSchedulerTest
    {
        public delegate void TaskForTaskScheduler();


        public TaskScheduler GetTaskScheduler(int numberOfTasks, TaskForTaskScheduler task)
        {
            var taskScheduler = new TaskScheduler();

            Action action = task.Invoke;

            for (int i = 0; i < numberOfTasks; i++)
            {
                taskScheduler.Add(action);
            }

            return taskScheduler;
        }

        public void PrintRandomNumberAndSleepOneSecond()
        {
            var random = new Random();
            var number = random.Next(999);
            Console.WriteLine(number);
            Thread.Sleep(1000);
        }

        [TestMethod]
        public void TaskSchedulerSuccessWorkTest()
        {
            var taskScheduler = GetTaskScheduler(15, PrintRandomNumberAndSleepOneSecond);

            var threadsCount = 3;
            taskScheduler.Start(threadsCount);
            taskScheduler.LetTheSchedulerFinishCurrentSession();
            Assert.AreEqual(0, taskScheduler.RunningTasksCount);
        }

        [TestMethod]
        public void SuccessfulParallelOperationOfTheTaskSchedulerTest()
        {
            var timer = new Stopwatch();
            var taskScheduler = GetTaskScheduler(6, PrintRandomNumberAndSleepOneSecond);

            var threadsCount = 3;

            timer.Start();
            taskScheduler.Start(threadsCount);
            taskScheduler.LetTheSchedulerFinishCurrentSession();
            timer.Stop();

            Console.WriteLine("Задачи выполнены за " + timer.ElapsedMilliseconds);

            Assert.AreEqual(0, taskScheduler.RunningTasksCount);
            Assert.AreEqual(0, taskScheduler.Amount);
            Assert.IsTrue(timer.ElapsedMilliseconds<=2500);
        }

        [TestMethod]
        public void StoppingWorkTaskSchedulerWhenTheQueueIsNotEmptyTest()
        {
            var taskScheduler = GetTaskScheduler(16, PrintRandomNumberAndSleepOneSecond);

            var threadsCount = 3;
            
            taskScheduler.Start(threadsCount);
            Thread.Sleep(3000);
            int amount = taskScheduler.Amount;
            taskScheduler.Stop();


            Assert.AreEqual(0, taskScheduler.RunningTasksCount);
            Assert.AreEqual(amount, taskScheduler.Amount);
        }


        [TestMethod]
        public void AddingTasksInTaskSchedulerWhenItWorksTest()
        {
            var taskScheduler = GetTaskScheduler(16, PrintRandomNumberAndSleepOneSecond);

            var threadsCount = 3;

            
            taskScheduler.Start(threadsCount);
            
            Thread.Sleep(1500);
            
            var amount = taskScheduler.Amount;

            taskScheduler.Add(PrintRandomNumberAndSleepOneSecond);

            Assert.AreEqual(amount + 1, taskScheduler.Amount);

            Thread.Sleep(5000);

            Assert.AreEqual(0, taskScheduler.RunningTasksCount);
        }

        
    }
}
