using BonfireDB.Entities.Base;


namespace BonfireDB.Entities
{
    public class SeedlingInfo : Entity
    {
        
        /// <summary> Номер рассады </summary>
        public int SeedlingNumber { get; set; }

        /// <summary>  Номер материнского растения (в случае черенкования) </summary>
        public int MotherPlantId { get; set; }

        /// <summary> Дата посадки </summary>
        public DateTime LandingDate { get; set; }

        /// <summary> Фаза Луны в момент посадки </summary>
        public string LunarPhase { get; set; }

        /// <summary> Куда посажено (горшок, теплица) </summary>
        public string PlantPlace { get; set; }

        /// <summary> Дата всхода рассады </summary>
        public DateTime GerminationDate { get;set; }

        /// <summary> Дата начала карантина </summary>
        public DateTime QuarantineStartDate { get; set; }

        /// <summary>  Дата окончания карантина </summary>
        public DateTime QuarantineStopDate { get; set; }

        /// <summary> Поле комментариев - причина карантина </summary>
        public string QuarantineCause { get; set; }

        /// <summary> Поле комментариев - результат карантина </summary>
        public string QuarantineNote { get; set; }

        /// <summary> Источник рассады (куплено, посеяно, подарено, черенкование) </summary>
        public string SeedlingSource { get; set; }

        /// <summary> Комментарий - где куплено/ кем подарено </summary>
        public string Note { get; set; }

        /// <summary> Дата начала закаливания </summary>
        public DateTime QuenchingDate { get; set; }

        /// <summary> Причина гибели (болезнь, физическая гибель, не развилось) </summary>
        public string DeathNote { get; set; }

        /// <summary> Список пересадок </summary>
        public List<Replanting> Replants { get; set; }

        /// <summary> Список обработки </summary>
        public List<Treatment> Treatments { get; set; }


        //В окне посадки должно быть:
        // 1 картинка с текущей фазой луны
        // 2 Культура
        // 3 Сорт
        // 4 Производитель ( в случае если рассада куплена/подарена, если нет производителя, то показать комментарий где куплено/кем подарено)
        // 5 Количество посаженных семян
        // 6 Дата посадки
        // 7 Фаза луны
        // 8 Количество всходов
        // 9 Дата всходов
        // 10 Количество погибшей рассады
        // 11 Остаток рассады
        // 12 Окошко посадки рассады
        // 13 Окошко расхода рассады (высажено, подарено, выброшено)


        // Отчет:
        // Общее количество по культуре
        // Расшифровка по каждому сорту с количеством



    }
}


