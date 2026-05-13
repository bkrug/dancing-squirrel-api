module DanceType.Models

open System.Threading.Tasks
open GenericModels
open DbLayer.Database

type DanceTypeSelector<'a> = Task<Result<seq<main.DanceType>, GenericModelResponse<'a>>>
type TeachersByDanceTypeSelector<'a> = int64 -> Task<Result<seq<main.Teacher>, GenericModelResponse<'a>>>
