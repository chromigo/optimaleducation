﻿using OptimalEducation.DAL.Models;
using OptimalEducation.Logic.Clusterizer;
using System;
using System.Collections.Generic;
using System.Data.Entity.Spatial;
using System.Linq;
using System.Web;

namespace OptimalEducation.Logic.AnalyticHierarchyProcess
{
    /// <summary>
    /// Расчет приоритетности пользователей с помощью метода анализа иерарохий относительно направления
    /// </summary>
    public class AHPEducationLine
    {
        EducationLine _educationLine;
        OptimalEducationDbContext context = new OptimalEducationDbContext();


        #region Общие настройки метода и приоритеты критериев

        double firstCriterionPriority = 0.40;
        double secondCriterionPriority = 0.35;
        double thirdCriterionPriority = 0.25;

        #endregion

        
        #region Переменные для первого критерия - сложности на основе ЕГЭ
        List<FirstCriterionUnit> FirstCriterionContainer = new List<FirstCriterionUnit>();
        int firstCriterionMatrixSize = 0;
        int edLineRequiredSum = 0;
        class FirstCriterionUnit
        {
            public int databaseId;
            public int matrixId;
            public int entrantSum;

            public double localPriority;
        }
        #endregion
        

        #region Переменные и классы для сложения критериев в конечную оценку
        public List<TotalResultUnit> AllCriterionContainer = new List<TotalResultUnit>();
        public class TotalResultUnit
        {
            public int databaseId;
            public double firstCriterionFinalPriority = 0;
            public double secondCriterionFinalPriority = 0;
            public double thirdCriterionFinalPriority = 0;

            public double absolutePriority = 0;
        }
        #endregion


        public AHPEducationLine(int edLineID, Dictionary<string, double> settings)
        {
            _educationLine = context.EducationLines.Find(edLineID);
            CalculateAll();
        }


        public AHPEducationLine(int edLineID)
        {
            _educationLine = context.EducationLines.Find(edLineID);
            CalculateAll();
        }


        void CalculateAll()
        {
            foreach (EducationLineRequirement EdLineReq in _educationLine.EducationLinesRequirements)
            {
                edLineRequiredSum += Convert.ToInt32(EdLineReq.Requirement);
            }

            //Console.WriteLine("Total summ: " + edLineRequiredSum.ToString());

            if (firstCriterionPriority > 0)
            {
                InitialiseFirstCriterion();
                //CalculateFirstCriterion();

                FinalCalculate();
            }
        }


        //Критерий трудности по ЕГЭ - заполнение направлений во временный список
        private void InitialiseFirstCriterion()
        {
            int counter = 0;

            foreach (Entrant entrant in context.Entrants)
            {
                int entrExamSum = 0;
                bool edLineAcceptable = true;

                foreach (EducationLineRequirement EdLineReq in _educationLine.EducationLinesRequirements)
                {
                    bool foundResult = false;

                    foreach (UnitedStateExam EntrExam in entrant.UnitedStateExams)
                    {
                        if (EntrExam.ExamDisciplineId == EdLineReq.ExamDisciplineId)
                        {
                            //Console.WriteLine("****** Entrant has " + EntrExam.Result.ToString());
                            foundResult = true;
                            entrExamSum = entrExamSum + Convert.ToInt32(EntrExam.Result);
                            break;
                        }
                    }

                    if (foundResult == false)
                    {
                        edLineAcceptable = false;
                        //Console.WriteLine("****** Entrant has no such exam");
                        break;
                    }
                }
                if (edLineAcceptable == false)
                {
                    //Console.WriteLine("====== NOT ACCEPTABLE EXAMS");

                    FirstCriterionUnit User = new FirstCriterionUnit();
                    User.databaseId = Convert.ToInt32(entrant.Id);
                    User.localPriority = 0;

                    FirstCriterionContainer.Add(User);
                }
                else
                {
                    //Console.WriteLine("====== ENTRANT HAS TOTAL OF " + entrExamSum.ToString());

                    FirstCriterionUnit User = new FirstCriterionUnit();
                    User.databaseId = Convert.ToInt32(entrant.Id);
                    User.matrixId = counter;
                    User.entrantSum = entrExamSum;
                    User.localPriority = 0;

                    //Console.WriteLine(">>>>>>>> WEAK DIFF: " + EducationLine.weakenedDifficulty);

                    counter++;

                    FirstCriterionContainer.Add(User);
                }
            }

            firstCriterionMatrixSize = counter;
            Console.WriteLine("TOTAL USERS TO GO: " + counter.ToString());
            Console.WriteLine("TOTAL USERS IN CONTAINER: " + FirstCriterionContainer.Count.ToString());
        }



        //Критерий трудности по ЕГЭ - расчеты приоритетов для всех подхолдящий направлений
        //private void CalculateFirstCriterion()
        //{
        //    //Console.WriteLine();
        //    //Console.WriteLine("======================================================================");
        //    //Console.WriteLine("======================================================================");
        //    //Console.WriteLine();

        //    double[,] pairwiseComparisonMatrix = new double[firstCriterionMatrixSize, firstCriterionMatrixSize];

        //    for (int i = 0; i < firstCriterionMatrixSize; i++)
        //    {
        //        for (int j = 0; j < firstCriterionMatrixSize; j++)
        //        {
        //            int a = FirstCriterionContainer.Find(x => x.matrixId == i).weakenedDifficulty;
        //            int b = FirstCriterionContainer.Find(y => y.matrixId == j).weakenedDifficulty;
        //            pairwiseComparisonMatrix[i, j] = FirstCriterionCompare(a, b);
        //        }
        //    }

        //    double[] resultVector = CalcEigenvectors(pairwiseComparisonMatrix, firstCriterionMatrixSize);

        //    //Console.WriteLine();

        //    for (int i = 0; i < firstCriterionMatrixSize; i++)
        //    {
        //        FirstCriterionContainer.Find(x => x.matrixId == i).localPriority = resultVector[i];
        //    }
        //    //Первый критерий закончил рачет приоритетов (локальных)

        //}







        //Расчет конечных приоритетов и сортировка
        private void FinalCalculate()
        {
            //Пролистываем результатты первого критерия и добавляем в общий список (учитывая приогритет критерия)
            for (int i = 0; i < FirstCriterionContainer.Count; i++)
            {
                if ((AllCriterionContainer.FindIndex(x => x.databaseId == FirstCriterionContainer[i].databaseId)) >= 0)
                {
                    //DUNNO LOL
                }
                else
                {
                    TotalResultUnit EducationLineFinal = new TotalResultUnit();
                    EducationLineFinal.databaseId = FirstCriterionContainer[i].databaseId;
                    AllCriterionContainer.Add(EducationLineFinal);
                }

                AllCriterionContainer.Find(x => x.databaseId == FirstCriterionContainer[i].databaseId).firstCriterionFinalPriority =
                    FirstCriterionContainer[i].localPriority * firstCriterionPriority;
            }

            //Завершающее сложение и заполнение выходного словарая
            foreach (TotalResultUnit UserFinal in AllCriterionContainer)
            {
                UserFinal.absolutePriority = UserFinal.firstCriterionFinalPriority + UserFinal.secondCriterionFinalPriority + UserFinal.thirdCriterionFinalPriority;
            }

            //Сортировка
            AllCriterionContainer.Sort((x, y) => y.absolutePriority.CompareTo(x.absolutePriority));

        }



    }
}
